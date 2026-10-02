using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/market-data")]
public sealed class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly ITechnicalAnalysisService _technicalAnalysisService;

    public MarketDataController(IMarketDataService marketDataService, ITechnicalAnalysisService technicalAnalysisService)
    {
        _marketDataService = marketDataService;
        _technicalAnalysisService = technicalAnalysisService;
    }

    [HttpGet("candles")]
    public async Task<IActionResult> GetHistoricalCandles(
        [FromQuery] string instrumentKey, [FromQuery] string unit, [FromQuery] int interval,
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken cancellationToken)
    {
        var result = await _marketDataService.GetHistoricalCandlesAsync(
            instrumentKey, unit, interval, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("intraday")]
    public async Task<IActionResult> GetIntradayCandles(
        [FromQuery] string instrumentKey, [FromQuery] string unit = "minutes",
        [FromQuery] int interval = 1, CancellationToken cancellationToken = default)
    {
        var result = await _marketDataService.GetIntradayCandlesAsync(
            instrumentKey, unit, interval, cancellationToken);
        return Ok(result);
    }

    [HttpGet("analysis")]
    public async Task<IActionResult> GetTechnicalAnalysis(
        [FromQuery] string instrumentKey,
        [FromQuery] string unit = "minutes",
        [FromQuery] int interval = 1,
        [FromQuery] bool intraday = true,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var candles = intraday
            ? await _marketDataService.GetIntradayCandlesAsync(instrumentKey, unit, interval, cancellationToken)
            : await GetHistoricalCandlesForAnalysis(instrumentKey, unit, interval, fromDate, toDate, cancellationToken);
        return Ok(_technicalAnalysisService.Analyze(candles.InstrumentKey, candles.Candles));
    }

    private Task<Quantora.Application.DTOs.MarketData.MarketCandlesResponseDto> GetHistoricalCandlesForAnalysis(
        string instrumentKey, string unit, int interval, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        if (!fromDate.HasValue || !toDate.HasValue)
            throw new ArgumentException("fromDate and toDate are required when intraday=false.");
        return _marketDataService.GetHistoricalCandlesAsync(
            instrumentKey, unit, interval, fromDate.Value, toDate.Value, cancellationToken);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchStocks(
        [FromQuery] string query, CancellationToken cancellationToken)
    {
        var results = await _marketDataService.SearchInstrumentsAsync(query, cancellationToken);
        return Ok(results);
    }

    [HttpGet("discover")]
    public async Task<IActionResult> Discover(CancellationToken cancellationToken)
    {
        var result = await _marketDataService.GetMarketDiscoveryAsync(cancellationToken);
        return Ok(result);
    }
}
