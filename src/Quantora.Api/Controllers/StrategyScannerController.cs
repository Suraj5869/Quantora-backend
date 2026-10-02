using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/strategy-scanner")]
public sealed class StrategyScannerController : ControllerBase
{
    private readonly IMarketDataService _marketData;
    private readonly ITechnicalAnalysisService _analysis;

    public StrategyScannerController(IMarketDataService marketData, ITechnicalAnalysisService analysis)
    {
        _marketData = marketData;
        _analysis = analysis;
    }

    /// <summary>
    /// Scans the currently configured featured-stock universe (20 instruments) for technical setups.
    /// This is a research aid, not a trade recommendation or an order-execution endpoint.
    /// </summary>
    [HttpGet("scan")]
    public async Task<IActionResult> Scan(CancellationToken cancellationToken)
    {
        var discovery = await _marketData.GetMarketDiscoveryAsync(cancellationToken);
        var instruments = discovery.FeaturedStocks
            .Where(x => !string.IsNullOrWhiteSpace(x.InstrumentKey))
            .GroupBy(x => x.InstrumentKey, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToArray();

        if (instruments.Length == 0)
            return Ok(new StrategyScanResponse(DateTimeOffset.UtcNow, 0, 0, Array.Empty<StrategyScanItem>(),
                "The configured featured-stock universe returned no instruments."));

        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = toDate.AddDays(-120);
        var results = new List<StrategyScanItem>(instruments.Length);

        foreach (var instrument in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await _marketData.GetHistoricalCandlesAsync(
                    instrument.InstrumentKey, "days", 1, fromDate, toDate, cancellationToken);
                var candles = response.Candles.OrderBy(c => c.Timestamp).ToArray();
                if (candles.Length < 55)
                {
                    results.Add(new StrategyScanItem(instrument.Name, instrument.TradingSymbol,
                        instrument.InstrumentKey, null, null, null, "Insufficient data",
                        "At least 55 daily candles are required.", candles.Length));
                    continue;
                }

                var analysis = _analysis.Analyze(instrument.InstrumentKey, candles);
                var latestClose = candles[^1].Close;
                var priorClose = candles[^2].Close;
                var dailyChange = priorClose > 0 ? (latestClose - priorClose) / priorClose * 100m : 0m;
                var setup = GetSetup(analysis.Trend, analysis.Momentum, analysis.Rsi14);
                var note = setup switch
                {
                    "Bullish setup" => "Trend and momentum currently align positively; confirm liquidity and risk before paper testing.",
                    "Bearish setup" => "Trend and momentum currently align negatively; this scanner does not open short positions.",
                    "Mixed signals" => "Trend and momentum do not align clearly; no directional setup identified.",
                    _ => "Insufficient indicator data for a directional setup."
                };

                results.Add(new StrategyScanItem(instrument.Name, instrument.TradingSymbol,
                    instrument.InstrumentKey, latestClose, decimal.Round(dailyChange, 2),
                    analysis.Rsi14.HasValue ? decimal.Round(analysis.Rsi14.Value, 2) : null,
                    setup, note, candles.Length));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Keep a per-instrument failure visible without failing the entire scan.
                results.Add(new StrategyScanItem(instrument.Name, instrument.TradingSymbol,
                    instrument.InstrumentKey, null, null, null, "Unavailable",
                    "Market data could not be loaded for this instrument. Check the connection and try again.",
                    0));
            }
        }

        var ordered = results
            .OrderBy(x => x.Setup == "Bullish setup" ? 0 : x.Setup == "Bearish setup" ? 1 : x.Setup == "Mixed signals" ? 2 : 3)
            .ThenByDescending(x => x.DailyChangePercent ?? decimal.MinValue)
            .ToArray();

        return Ok(new StrategyScanResponse(DateTimeOffset.UtcNow, instruments.Length, ordered.Count(x => x.Setup != "Unavailable"),
            ordered, "Scans the configured 20-stock featured universe, not the complete Nifty 50. Uses daily candles and SMA20/SMA50 trend plus RSI/MACD momentum. Results are informational only; no orders are placed."));
    }

    private static string GetSetup(string trend, string momentum, decimal? rsi)
    {
        if (trend == "Bullish" && momentum == "Positive" && rsi is > 30 and < 70)
            return "Bullish setup";
        if (trend == "Bearish" && momentum == "Negative" && rsi is > 30 and < 70)
            return "Bearish setup";
        if (trend == "Insufficient data" || momentum == "Insufficient data")
            return "Insufficient data";
        return "Mixed signals";
    }
}

public sealed record StrategyScanItem(
    string Name,
    string TradingSymbol,
    string InstrumentKey,
    decimal? LastClose,
    decimal? DailyChangePercent,
    decimal? Rsi14,
    string Setup,
    string Note,
    int CandleCount);

public sealed record StrategyScanResponse(
    DateTimeOffset ScannedAt,
    int UniverseSize,
    int SuccessfulResults,
    IReadOnlyList<StrategyScanItem> Results,
    string ScopeAndDisclaimer);
