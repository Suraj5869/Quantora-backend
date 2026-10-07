using Quantora.Application.DTOs.News;

namespace Quantora.Application.Interfaces;

public interface INewsRepository
{
    Task UpsertAsync(IReadOnlyList<NewsArticleDto> articles, CancellationToken cancellationToken = default);
    Task<NewsListResponse> SearchAsync(NewsQuery query, CancellationToken cancellationToken = default);
    Task<NewsAnalysisDto> GetAnalysisAsync(string ticker, CancellationToken cancellationToken = default);
}
