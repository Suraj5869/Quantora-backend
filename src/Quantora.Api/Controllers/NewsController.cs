using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.DTOs.News;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/news")]
public sealed class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService) => _newsService = newsService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] string? ticker,
        [FromQuery] string? category,
        [FromQuery] string? sentiment,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string sort = "latest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _newsService.GetNewsAsync(new NewsQuery
        {
            Search = search, Ticker = ticker, Category = category, Sentiment = sentiment,
            FromDate = fromDate, ToDate = toDate, Sort = sort, Page = page, PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        => Ok(await _newsService.RefreshAsync(cancellationToken));

    [HttpGet("analysis/{ticker}")]
    public async Task<IActionResult> Analysis(string ticker, CancellationToken cancellationToken)
        => Ok(await _newsService.GetAnalysisAsync(ticker, cancellationToken));
}
