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
        var normalized = query with
        {
            Page = Math.Clamp(query.Page, 1, 1000),
            PageSize = Math.Clamp(query.PageSize, 1, 50)
        };
        return await _repository.SearchAsync(normalized, cancellationToken);
    }

    public async Task<NewsRefreshResponse> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var articles = await _provider.FetchLatestAsync(cancellationToken);
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
