using Dapper;
using Npgsql;
using Quantora.Application.DTOs.News;
using Quantora.Application.Interfaces;
using Quantora.Infrastructure.Persistence;

namespace Quantora.Infrastructure.Repositories;

public sealed class NewsRepository : INewsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    public NewsRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task UpsertAsync(IReadOnlyList<NewsArticleDto> articles, CancellationToken cancellationToken = default)
    {
        if (articles.Count == 0) return;
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO stocks.news_articles
            (id,source_name,title,summary,url,image_url,published_at,fetched_at,category,sentiment,sentiment_score,impact,relevance_score,tickers,content_hash)
            VALUES (@Id,@SourceName,@Title,@Summary,@Url,@ImageUrl,@PublishedAt,now(),@Category,@Sentiment,@SentimentScore,@Impact,@RelevanceScore,@Tickers,@ContentHash)
            ON CONFLICT(content_hash) DO UPDATE SET
              source_name=EXCLUDED.source_name, summary=EXCLUDED.summary, image_url=EXCLUDED.image_url,
              category=EXCLUDED.category, sentiment=EXCLUDED.sentiment, sentiment_score=EXCLUDED.sentiment_score,
              impact=EXCLUDED.impact, relevance_score=EXCLUDED.relevance_score, tickers=EXCLUDED.tickers;
            """;
        foreach (var article in articles)
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(article.Title.Trim().ToLowerInvariant() + "|" + article.Url.Trim()))).ToLowerInvariant();
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                article.Id, article.SourceName, article.Title, article.Summary, article.Url, article.ImageUrl,
                article.PublishedAt, article.Category, article.Sentiment, article.SentimentScore, article.Impact,
                article.RelevanceScore, Tickers = article.Tickers.ToArray(), ContentHash = hash
            }, cancellationToken: cancellationToken));
        }
    }

    public async Task<NewsListResponse> SearchAsync(NewsQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            where.Add("(title ILIKE @Search OR summary ILIKE @Search OR source_name ILIKE @Search OR @Search = ANY(tickers))");
            parameters.Add("Search", "%" + query.Search.Trim().Replace("%", "\\%").Replace("_", "\\_") + "%");
        }
        if (!string.IsNullOrWhiteSpace(query.Ticker))
        {
            where.Add("@Ticker = ANY(tickers)");
            parameters.Add("Ticker", query.Ticker.Trim().ToUpperInvariant());
        }
        if (!string.IsNullOrWhiteSpace(query.Category) && !string.Equals(query.Category, "All", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("category = @Category"); parameters.Add("Category", query.Category);
        }
        if (!string.IsNullOrWhiteSpace(query.Sentiment) && !string.Equals(query.Sentiment, "All", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("sentiment = @Sentiment"); parameters.Add("Sentiment", query.Sentiment);
        }
        if (query.FromDate.HasValue) { where.Add("published_at >= @FromDate"); parameters.Add("FromDate", query.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)); }
        if (query.ToDate.HasValue) { where.Add("published_at < @ToDate"); parameters.Add("ToDate", query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)); }

        var predicate = where.Count == 0 ? "" : "WHERE " + string.Join(" AND ", where);
        var order = string.Equals(query.Sort, "oldest", StringComparison.OrdinalIgnoreCase) ? "published_at ASC" : "published_at DESC";
        var offset = (Math.Max(query.Page, 1) - 1) * Math.Clamp(query.PageSize, 1, 50);
        parameters.Add("Offset", offset); parameters.Add("Limit", Math.Clamp(query.PageSize, 1, 50));

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT COUNT(*) FROM stocks.news_articles {predicate};", parameters, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<NewsArticleRow>(new CommandDefinition($"""
            SELECT id,source_name,title,COALESCE(summary,'') summary,url,image_url,published_at,category,sentiment,
                   sentiment_score,impact,relevance_score,tickers
            FROM stocks.news_articles {predicate}
            ORDER BY {order}
            OFFSET @Offset LIMIT @Limit;
            """, parameters, cancellationToken: cancellationToken));

        return new NewsListResponse
        {
            Items = rows.Select(ToDto).ToArray(), Page = Math.Max(query.Page, 1),
            PageSize = Math.Clamp(query.PageSize, 1, 50), TotalCount = total, FetchedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<NewsAnalysisDto> GetAnalysisAsync(string ticker, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<NewsAnalysisRow>(new CommandDefinition("""
            SELECT title, sentiment_score, impact, published_at
            FROM stocks.news_articles
            WHERE @Ticker = ANY(tickers) AND published_at >= now() - interval '48 hours'
            ORDER BY published_at DESC LIMIT 30;
            """, new { Ticker = ticker }, cancellationToken: cancellationToken));
        var list = rows.AsList();
        if (list.Count == 0)
            return new NewsAnalysisDto { Ticker = ticker, Sentiment = "Neutral", Impact = "Low" };

        decimal weightSum = 0, weighted = 0;
        foreach (var row in list)
        {
            var ageHours = Math.Max(0, (DateTimeOffset.UtcNow - row.PublishedAt).TotalHours);
            var recency = (decimal)Math.Exp(-ageHours / 24.0);
            var impact = row.Impact switch { "High" => 1.25m, "Medium" => 1m, _ => 0.75m };
            var weight = recency * impact;
            weighted += row.SentimentScore * weight; weightSum += weight;
        }
        var score = weightSum == 0 ? 0 : decimal.Round(weighted / weightSum * 100m, 1);
        return new NewsAnalysisDto
        {
            Ticker = ticker, ArticleCount = list.Count, Score = score,
            Sentiment = score >= 20 ? "Positive" : score <= -20 ? "Negative" : "Neutral",
            Impact = list.Any(x => x.Impact == "High") ? "High" : list.Any(x => x.Impact == "Medium") ? "Medium" : "Low",
            KeyHeadlines = list.Take(5).Select(x => x.Title).ToArray()
        };
    }

    private static NewsArticleDto ToDto(NewsArticleRow x) => new()
    {
        Id=x.Id, SourceName=x.SourceName, Title=x.Title, Summary=x.Summary, Url=x.Url, ImageUrl=x.ImageUrl,
        PublishedAt=x.PublishedAt, Category=x.Category, Sentiment=x.Sentiment, SentimentScore=x.SentimentScore,
        Impact=x.Impact, RelevanceScore=x.RelevanceScore, Tickers=x.Tickers ?? Array.Empty<string>()
    };

    private sealed class NewsArticleRow
    {
        public Guid Id { get; init; } public string SourceName { get; init; } = ""; public string Title { get; init; } = "";
        public string Summary { get; init; } = ""; public string Url { get; init; } = ""; public string? ImageUrl { get; init; }
        public DateTimeOffset PublishedAt { get; init; } public string Category { get; init; } = "Market";
        public string Sentiment { get; init; } = "Neutral"; public decimal SentimentScore { get; init; }
        public string Impact { get; init; } = "Low"; public decimal RelevanceScore { get; init; } public string[]? Tickers { get; init; }
    }
    private sealed class NewsAnalysisRow { public string Title { get; init; } = ""; public decimal SentimentScore { get; init; } public string Impact { get; init; } = "Low"; public DateTimeOffset PublishedAt { get; init; } }
}
