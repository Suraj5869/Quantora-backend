using Quantora.Application.DTOs.News;

namespace Quantora.Application.Interfaces;

public interface INewsProvider
{
    Task<IReadOnlyList<NewsArticleDto>> FetchLatestAsync(CancellationToken cancellationToken = default);
}
