namespace Quantora.Application.DTOs.News;

public sealed class NewsArticleDto
{
    public Guid Id { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string Category { get; init; } = "Market";
    public string Sentiment { get; init; } = "Neutral";
    public decimal SentimentScore { get; init; }
    public string Impact { get; init; } = "Low";
    public decimal RelevanceScore { get; init; }
    public IReadOnlyList<string> Tickers { get; init; } = Array.Empty<string>();
}

public sealed class NewsQuery
{
    public string? Search { get; init; }
    public string? Ticker { get; init; }
    public string? Category { get; init; }
    public string? Sentiment { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public string Sort { get; init; } = "latest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class NewsListResponse
{
    public IReadOnlyList<NewsArticleDto> Items { get; init; } = Array.Empty<NewsArticleDto>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
}

public sealed class NewsRefreshResponse
{
    public int Added { get; init; }
    public int Updated { get; init; }
    public DateTimeOffset RefreshedAt { get; init; }
}

public sealed class NewsAnalysisDto
{
    public string Ticker { get; init; } = string.Empty;
    public int ArticleCount { get; init; }
    public decimal Score { get; init; }
    public string Sentiment { get; init; } = "Neutral";
    public string Impact { get; init; } = "Low";
    public IReadOnlyList<string> KeyHeadlines { get; init; } = Array.Empty<string>();
}
