using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.DTOs.PaperTrading;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/paper-trading/automation")]
public sealed class PaperTradingAutomationController : ControllerBase
{
    private const decimal MaximumRiskPercent = 1m;
    private const int MaximumNewTradesPerRun = 3;
    private readonly IMarketDataService _marketData;
    private readonly ITechnicalAnalysisService _analysis;
    private readonly IPaperTradingService _paperTrading;
    private readonly INewsService _news;

    public PaperTradingAutomationController(
        IMarketDataService marketData,
        ITechnicalAnalysisService analysis,
        IPaperTradingService paperTrading,
        INewsService news)
    {
        _marketData = marketData;
        _analysis = analysis;
        _paperTrading = paperTrading;
        _news = news;
    }

    /// <summary>
    /// Runs one user-triggered scan and places eligible paper buys only.
    /// It never submits real broker orders and is not a background scheduler.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var discovery = await _marketData.GetMarketDiscoveryAsync(cancellationToken);
        var universe = discovery.FeaturedStocks
            .Where(x => !string.IsNullOrWhiteSpace(x.InstrumentKey))
            .GroupBy(x => x.InstrumentKey, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToArray();

        if (universe.Length == 0)
            return Ok(new PaperAutomationRunResponse(DateTimeOffset.UtcNow, 0, 0, 0,
                Array.Empty<PaperAutomationRunItem>(),
                "No instruments were returned by the configured featured-stock universe."));

        var account = await _paperTrading.GetAccountAsync(cancellationToken);
        var heldKeys = account.Positions.Select(p => p.InstrumentKey).ToHashSet(StringComparer.Ordinal);
        var results = new List<PaperAutomationRunItem>(universe.Length);
        var newTrades = 0;
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = toDate.AddDays(-120);

        foreach (var stock in universe)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (newTrades >= MaximumNewTradesPerRun)
            {
                results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                    "Skipped", "Maximum of three new paper entries per run reached.", null, null));
                continue;
            }

            if (heldKeys.Contains(stock.InstrumentKey))
            {
                results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                    "Skipped", "An open paper position already exists for this instrument.", null, null));
                continue;
            }

            try
            {
                var history = await _marketData.GetHistoricalCandlesAsync(
                    stock.InstrumentKey, "days", 1, fromDate, toDate, cancellationToken);
                var candles = history.Candles.OrderBy(c => c.Timestamp).ToArray();
                if (candles.Length < 55)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Skipped", "At least 55 daily candles are required.", null, null));
                    continue;
                }

                var analysis = _analysis.Analyze(stock.InstrumentKey, candles);
                var news = await _news.GetAnalysisAsync(stock.TradingSymbol, cancellationToken);
                var technicalScore = analysis.Trend == "Bullish" && analysis.Momentum == "Positive" ? 70m : 0m;
                if (analysis.Rsi14 is >= 40m and <= 68m) technicalScore += 20m;
                if (analysis.Macd.HasValue && analysis.MacdSignal.HasValue && analysis.Macd > analysis.MacdSignal) technicalScore += 10m;
                var overallScore = decimal.Round(technicalScore * 0.70m + Math.Clamp(news.Score, -100m, 100m) * 0.30m, 1);
                if (analysis.Trend != "Bullish" || analysis.Momentum != "Positive" ||
                    analysis.Rsi14 is null || analysis.Rsi14 < 40m || analysis.Rsi14 > 68m || news.Score < -20m)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "No setup",
                        $"Technical/news rules not met. Technical score {technicalScore:0.0}, news score {news.Score:0.0}, combined score {overallScore:0.0}. Recent news must not be materially negative.",
                        null, null));
                    continue;
                }

                if (analysis.Atr14 is null || analysis.Atr14 <= 0)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Skipped", "ATR is unavailable; safe position sizing could not be calculated.", null, null));
                    continue;
                }

                var intraday = await _marketData.GetIntradayCandlesAsync(
                    stock.InstrumentKey, "minutes", 1, cancellationToken);
                var current = intraday.Candles.OrderByDescending(c => c.Timestamp).FirstOrDefault();
                if (current is null || current.Close <= 0)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Skipped", "Current market price is unavailable.", null, null));
                    continue;
                }

                var stopDistance = decimal.Round(analysis.Atr14.Value * 2m, 2, MidpointRounding.AwayFromZero);
                var stopPrice = decimal.Round(current.Close - stopDistance, 2, MidpointRounding.AwayFromZero);
                if (stopDistance <= 0 || stopPrice <= 0)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Skipped", "ATR stop price is not valid for this instrument.", null, null));
                    continue;
                }

                account = await _paperTrading.GetAccountAsync(cancellationToken);
                var riskBudget = decimal.Round(account.PortfolioValue * MaximumRiskPercent / 100m, 2);
                var riskQuantity = (int)Math.Min(1_000_000m, decimal.Floor(riskBudget / stopDistance));
                var cashQuantity = (int)Math.Min(1_000_000m, decimal.Floor(account.AvailableCash / current.Close));
                var quantity = Math.Min(riskQuantity, cashQuantity);
                if (quantity < 1)
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Skipped", "Available cash or the 1% risk budget is too small for one whole share.", null, null));
                    continue;
                }

                var order = await _paperTrading.PlaceOrderAsync(new PlacePaperOrderRequest
                {
                    InstrumentKey = stock.InstrumentKey,
                    TradingSymbol = stock.TradingSymbol,
                    Side = "BUY",
                    Quantity = quantity,
                    StopLossPrice = stopPrice
                }, cancellationToken);

                if (order.Status == "FILLED")
                {
                    newTrades++;
                    heldKeys.Add(stock.InstrumentKey);
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Paper order filled", $"Bought {quantity} share(s) using the 2x ATR stop. Technical score {technicalScore:0.0}, news score {news.Score:0.0}, combined score {overallScore:0.0}. News sentiment: {news.Sentiment}. Planned risk is capped at 1% of current paper equity.",
                        order.ExecutionPrice, stopPrice));
                }
                else
                {
                    results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                        "Rejected", order.RejectionReason ?? "Paper order was not filled.", order.ExecutionPrice, stopPrice));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new PaperAutomationRunItem(stock.TradingSymbol, stock.InstrumentKey,
                    "Unavailable", "Market data or paper-order processing failed for this instrument; no real broker order was sent.", null, null));
            }
        }

        var message = $"Reviewed {universe.Length} instruments from the configured featured-stock universe. This is not the complete Nifty 50. Up to {MaximumNewTradesPerRun} new paper positions are opened per run.";
        return Ok(new PaperAutomationRunResponse(DateTimeOffset.UtcNow, universe.Length,
            results.Count(x => x.Status == "Paper order filled"), results.Count(x => x.Status == "No setup"),
            results, message));
    }
}

public sealed record PaperAutomationRunResponse(
    DateTimeOffset RunAt,
    int InstrumentsReviewed,
    int OrdersFilled,
    int NoSetupCount,
    IReadOnlyList<PaperAutomationRunItem> Results,
    string Message);

public sealed record PaperAutomationRunItem(
    string TradingSymbol,
    string InstrumentKey,
    string Status,
    string Detail,
    decimal? ExecutionPrice,
    decimal? StopLossPrice);
