using Quantora.Application.DTOs.News;

namespace Quantora.Application.Services;

public interface INewsService
{
    Task<NewsListResponse> GetNewsAsync(NewsQuery query, CancellationToken cancellationToken = default);
    Task<NewsRefreshResponse> RefreshAsync(CancellationToken cancellationToken = default);
    Task<NewsAnalysisDto> GetAnalysisAsync(string ticker, CancellationToken cancellationToken = default);
}
