using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/backtesting")]
public sealed class BacktestingController : ControllerBase
{
    private readonly IMarketDataService _marketData;
    public BacktestingController(IMarketDataService marketData) => _marketData = marketData;

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] BacktestRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InstrumentKey) || !request.InstrumentKey.StartsWith("NSE_EQ|", StringComparison.Ordinal))
            return BadRequest(new { message = "A valid NSE equity instrument key is required." });
        if (request.FromDate >= request.ToDate || request.ToDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new { message = "Choose a valid historical date range ending no later than today." });
        if (request.ToDate.DayNumber - request.FromDate.DayNumber > 365)
            return BadRequest(new { message = "Backtests are limited to one year per run." });
        if (request.InitialCapital < 1000 || request.InitialCapital > 100000000)
            return BadRequest(new { message = "Initial capital must be between ₹1,000 and ₹10 crore." });
        if (request.RiskPercent <= 0 || request.RiskPercent > 1)
            return BadRequest(new { message = "Risk per trade must be greater than 0 and at most 1%." });

        var response = await _marketData.GetHistoricalCandlesAsync(
            request.InstrumentKey, "days", 1, request.FromDate, request.ToDate, cancellationToken);
        var candles = response.Candles.OrderBy(c => c.Timestamp).ToArray();
        if (candles.Length < 55)
            return BadRequest(new { message = "At least 55 daily candles are required for this strategy and date range." });

        var closes = candles.Select(c => c.Close).ToArray();
        var sma20 = Sma(closes, 20);
        var sma50 = Sma(closes, 50);
        var atr = Atr(candles, 14);
        decimal cash = request.InitialCapital;
        decimal qty = 0, entry = 0, stop = 0, target = 0;
        var trades = new List<BacktestTrade>();
        var equity = new List<BacktestEquityPoint>();
        var riskBudgetPercent = request.RiskPercent / 100m;

        for (var i = 50; i < candles.Length; i++)
        {
            var candle = candles[i];
            string? exitReason = null;
            decimal exitPrice = 0;
            if (qty > 0)
            {
                // Conservative assumption: if stop and target are both touched in one candle, assume stop first.
                if (candle.Low <= stop) { exitPrice = stop; exitReason = "Stop loss"; }
                else if (candle.High >= target) { exitPrice = target; exitReason = "Target"; }
                else if (sma20[i] < sma50[i] && sma20[i - 1] >= sma50[i - 1]) { exitPrice = candle.Close; exitReason = "Bearish crossover"; }
                if (exitReason is not null)
                {
                    var pnl = (exitPrice - entry) * qty;
                    cash += exitPrice * qty;
                    trades.Add(new BacktestTrade(candle.Timestamp, "SELL", qty, exitPrice, decimal.Round(pnl, 2), exitReason));
                    qty = 0;
                }
            }

            if (qty == 0 && i > 50 && sma20[i - 1] > sma50[i - 1] && sma20[i - 2] <= sma50[i - 2] && atr[i - 1] > 0)
            {
                // Signal uses the prior completed candle; fill at this candle's open to avoid look-ahead bias.
                var proposedEntry = candle.Open;
                var proposedStop = proposedEntry - (atr[i - 1] * 2m);
                var riskPerShare = proposedEntry - proposedStop;
                var riskBudget = cash * riskBudgetPercent;
                var riskQty = decimal.Floor(riskBudget / riskPerShare);
                var cashQty = decimal.Floor(cash / proposedEntry);
                var proposedQty = Math.Min(riskQty, cashQty);
                if (proposedQty >= 1)
                {
                    qty = proposedQty; entry = proposedEntry; stop = proposedStop; target = proposedEntry + riskPerShare * 2m;
                    cash -= qty * entry;
                    trades.Add(new BacktestTrade(candle.Timestamp, "BUY", qty, entry, 0, "SMA20 crossed above SMA50"));

                    // The entry candle can also hit the protective stop or target after the open.
                    // Apply the same conservative stop-first rule used for existing positions.
                    string? entryCandleExit = null;
                    decimal entryCandleExitPrice = 0;
                    if (candle.Low <= stop) { entryCandleExit = "Stop loss"; entryCandleExitPrice = stop; }
                    else if (candle.High >= target) { entryCandleExit = "Target"; entryCandleExitPrice = target; }
                    if (entryCandleExit is not null)
                    {
                        var entryCandlePnl = (entryCandleExitPrice - entry) * qty;
                        cash += entryCandleExitPrice * qty;
                        trades.Add(new BacktestTrade(candle.Timestamp, "SELL", qty, entryCandleExitPrice,
                            decimal.Round(entryCandlePnl, 2), entryCandleExit));
                        qty = 0;
                    }
                }
            }

            var markedEquity = cash + qty * candle.Close;
            equity.Add(new BacktestEquityPoint(candle.Timestamp, decimal.Round(markedEquity, 2)));
        }

        if (qty > 0)
        {
            var last = candles[^1];
            var pnl = (last.Close - entry) * qty;
            cash += last.Close * qty;
            trades.Add(new BacktestTrade(last.Timestamp, "SELL", qty, last.Close, decimal.Round(pnl, 2), "End of test period"));
            qty = 0;
        }

        var sellTrades = trades.Where(t => t.Side == "SELL").ToArray();
        var wins = sellTrades.Count(t => t.Pnl > 0);
        var grossProfit = sellTrades.Where(t => t.Pnl > 0).Sum(t => t.Pnl);
        var grossLoss = Math.Abs(sellTrades.Where(t => t.Pnl < 0).Sum(t => t.Pnl));
        var finalEquity = cash;
        var peak = request.InitialCapital;
        decimal maxDrawdown = 0;
        foreach (var point in equity)
        {
            peak = Math.Max(peak, point.Equity);
            if (peak > 0) maxDrawdown = Math.Max(maxDrawdown, (peak - point.Equity) / peak * 100m);
        }

        return Ok(new BacktestResponse
        {
            InstrumentKey = request.InstrumentKey, FromDate = request.FromDate, ToDate = request.ToDate,
            InitialCapital = request.InitialCapital, FinalEquity = decimal.Round(finalEquity, 2),
            NetPnl = decimal.Round(finalEquity - request.InitialCapital, 2),
            ReturnPercent = decimal.Round((finalEquity - request.InitialCapital) / request.InitialCapital * 100m, 2),
            MaxDrawdownPercent = decimal.Round(maxDrawdown, 2), TradeCount = sellTrades.Length,
            WinRatePercent = sellTrades.Length == 0 ? 0 : decimal.Round((decimal)wins / sellTrades.Length * 100m, 2),
            ProfitFactor = grossLoss == 0 ? (grossProfit > 0 ? null : 0) : decimal.Round(grossProfit / grossLoss, 2),
            Strategy = $"SMA 20/50 crossover; ATR(14) stop at 2x ATR; target at 2R; {request.RiskPercent:0.##}% risk per trade maximum",
            Assumptions =
            [
                "Long-only; daily candles; signals use completed candles and entries fill at the next candle open.",
                "If stop and target are both touched on the same candle, stop-loss is assumed first.",
                "No brokerage, taxes or slippage included; historical simulation only and no orders are sent to any broker."
            ],
            Trades = trades, EquityCurve = equity
        });
    }

    private static decimal[] Sma(decimal[] values, int period)
    {
        var result = new decimal[values.Length]; decimal sum = 0;
        for (var i = 0; i < values.Length; i++) { sum += values[i]; if (i >= period) sum -= values[i - period]; result[i] = i >= period - 1 ? sum / period : 0; }
        return result;
    }
    private static decimal[] Atr(IReadOnlyList<Quantora.Application.DTOs.MarketData.MarketCandleDto> data, int period)
    {
        var tr = new decimal[data.Count];
        for (var i = 0; i < data.Count; i++) { var prev = i == 0 ? data[i].Close : data[i - 1].Close; tr[i] = Math.Max(data[i].High - data[i].Low, Math.Max(Math.Abs(data[i].High - prev), Math.Abs(data[i].Low - prev))); }
        var result = new decimal[data.Count]; if (data.Count < period) return result;
        var current = tr.Take(period).Sum() / period; result[period - 1] = current;
        for (var i = period; i < tr.Length; i++) { current = (current * (period - 1) + tr[i]) / period; result[i] = current; }
        return result;
    }
}

public sealed class BacktestRequest
{
    public string InstrumentKey { get; init; } = string.Empty;
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public decimal InitialCapital { get; init; } = 100000m;
    public decimal RiskPercent { get; init; } = 1m;
}
public sealed class BacktestTrade(DateTimeOffset timestamp, string side, decimal quantity, decimal price, decimal pnl, string reason)
{
    public DateTimeOffset Timestamp { get; } = timestamp;
    public string Side { get; } = side;
    public decimal Quantity { get; } = quantity;
    public decimal Price { get; } = price;
    public decimal Pnl { get; } = pnl;
    public string Reason { get; } = reason;
}
public sealed class BacktestEquityPoint(DateTimeOffset timestamp, decimal equity)
{
    public DateTimeOffset Timestamp { get; } = timestamp;
    public decimal Equity { get; } = equity;
}
public sealed class BacktestResponse
{
    public string InstrumentKey { get; init; } = string.Empty;
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal FinalEquity { get; init; }
    public decimal NetPnl { get; init; }
    public decimal ReturnPercent { get; init; }
    public decimal MaxDrawdownPercent { get; init; }
    public int TradeCount { get; init; }
    public decimal WinRatePercent { get; init; }
    public decimal? ProfitFactor { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public IReadOnlyList<string> Assumptions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BacktestTrade> Trades { get; init; } = Array.Empty<BacktestTrade>();
    public IReadOnlyList<BacktestEquityPoint> EquityCurve { get; init; } = Array.Empty<BacktestEquityPoint>();
}
