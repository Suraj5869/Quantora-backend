using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/paper-trading/risk")]
public sealed class PaperTradingRiskController : ControllerBase
{
    private const decimal MaximumRiskPercent = 1m;
    private readonly IPaperTradingService _paperTrading;
    private readonly IMarketDataService _marketData;
    private readonly ITechnicalAnalysisService _technicalAnalysis;

    public PaperTradingRiskController(
        IPaperTradingService paperTrading,
        IMarketDataService marketData,
        ITechnicalAnalysisService technicalAnalysis)
    {
        _paperTrading = paperTrading;
        _marketData = marketData;
        _technicalAnalysis = technicalAnalysis;
    }

    /// <summary>
    /// Calculates a paper-trading position-size preview using a 2x ATR stop.
    /// This endpoint never places or modifies an order.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] string instrumentKey,
        [FromQuery] decimal riskPercent = 1m,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instrumentKey) ||
            instrumentKey.Length > 100 ||
            !instrumentKey.StartsWith("NSE_EQ|", StringComparison.Ordinal))
            return BadRequest(new { message = "A valid NSE equity instrument key is required." });

        if (riskPercent <= 0 || riskPercent > MaximumRiskPercent)
            return BadRequest(new { message = "Risk per trade must be greater than 0% and no more than 1%." });

        var account = await _paperTrading.GetAccountAsync(cancellationToken);
        if (account.PortfolioValue <= 0)
            return BadRequest(new { message = "Paper account equity must be positive before sizing a position." });

        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = toDate.AddDays(-120);
        var history = await _marketData.GetHistoricalCandlesAsync(
            instrumentKey, "days", 1, fromDate, toDate, cancellationToken);
        var candles = history.Candles.OrderBy(c => c.Timestamp).ToArray();

        if (candles.Length < 55)
            return BadRequest(new { message = "At least 55 daily candles are required to calculate the ATR-based stop." });

        var analysis = _technicalAnalysis.Analyze(instrumentKey, candles);
        var entryPrice = candles[^1].Close;
        if (entryPrice <= 0 || analysis.Atr14 is null || analysis.Atr14 <= 0)
            return BadRequest(new { message = "A valid price and ATR value are required to calculate position size." });

        var stopDistance = decimal.Round(analysis.Atr14.Value * 2m, 2, MidpointRounding.AwayFromZero);
        var stopPrice = decimal.Round(entryPrice - stopDistance, 2, MidpointRounding.AwayFromZero);
        if (stopDistance <= 0 || stopPrice <= 0)
            return BadRequest(new { message = "The calculated ATR stop is not valid for this instrument price." });

        var riskBudget = decimal.Round(account.PortfolioValue * riskPercent / 100m, 2);
        var riskBasedQuantity = decimal.ToInt32(decimal.Floor(riskBudget / stopDistance));
        var cashBasedQuantity = decimal.ToInt32(decimal.Floor(account.AvailableCash / entryPrice));
        var quantity = Math.Max(0, Math.Min(riskBasedQuantity, cashBasedQuantity));
        var estimatedCost = decimal.Round(quantity * entryPrice, 2);
        var plannedRisk = decimal.Round(quantity * stopDistance, 2);
        var openRisk = account.Positions.Sum(p =>
            Math.Max(0m, p.AveragePrice - p.LastPrice) * p.Quantity);

        return Ok(new PaperTradeRiskPreview(
            instrumentKey,
            DateTimeOffset.UtcNow,
            account.PortfolioValue,
            account.AvailableCash,
            riskPercent,
            riskBudget,
            entryPrice,
            analysis.Atr14.Value,
            stopPrice,
            stopDistance,
            quantity,
            estimatedCost,
            plannedRisk,
            openRisk,
            "Preview only: uses the latest available daily close as the reference entry, a 2x ATR stop, whole-share sizing, and available cash. Actual fills, gaps, slippage, fees, and changing prices can cause realized loss to exceed planned risk. This does not submit an order."));
    }
}

public sealed record PaperTradeRiskPreview(
    string InstrumentKey,
    DateTimeOffset CalculatedAt,
    decimal PortfolioEquity,
    decimal AvailableCash,
    decimal RiskPercent,
    decimal RiskBudget,
    decimal ReferenceEntryPrice,
    decimal Atr14,
    decimal StopPrice,
    decimal StopDistance,
    int SuggestedQuantity,
    decimal EstimatedCost,
    decimal PlannedRiskAtStop,
    decimal ExistingUnrealizedLossEstimate,
    string Disclaimer);
