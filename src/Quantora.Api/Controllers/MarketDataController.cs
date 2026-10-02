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

    public MarketDataController(IMarketDataService marketDataService) =>
        _marketDataService = marketDataService;

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
