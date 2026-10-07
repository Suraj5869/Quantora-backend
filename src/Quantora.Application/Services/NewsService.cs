using Quantora.Application.DTOs.News;
using Quantora.Application.Interfaces;

namespace Quantora.Application.Services;

public sealed class NewsService : INewsService
{
    private readonly INewsRepository _repository;
    private readonly INewsProvider _provider;

    public NewsService(INewsRepository repository, INewsProvider provider)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<NewsListResponse> GetNewsAsync(NewsQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Clamp(query.Page, 1, 1000);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        return await _repository.SearchAsync(query with { Page = page, PageSize = pageSize }, cancellationToken);
    }

    public async Task<NewsRefreshResponse> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var articles = await _provider.FetchLatestAsync(cancellationToken);
        if (articles.Count == 0)
            return new NewsRefreshResponse { RefreshedAt = DateTimeOffset.UtcNow };

        await _repository.UpsertAsync(articles, cancellationToken);
        return new NewsRefreshResponse
        {
            Added = articles.Count,
            Updated = 0,
            RefreshedAt = DateTimeOffset.UtcNow
        };
    }

    public Task<NewsAnalysisDto> GetAnalysisAsync(string ticker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticker) || ticker.Length > 20)
            throw new ArgumentException("A valid ticker is required.");
        return _repository.GetAnalysisAsync(ticker.Trim().ToUpperInvariant(), cancellationToken);
    }
}
