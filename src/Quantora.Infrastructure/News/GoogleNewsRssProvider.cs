using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Quantora.Application.DTOs.News;
using Quantora.Application.Interfaces;

namespace Quantora.Infrastructure.News;

public sealed class GoogleNewsRssProvider : INewsProvider
{
    private readonly HttpClient _httpClient;
    private static readonly (string Ticker,string Name)[] Universe =
    [
        ("RELIANCE","Reliance Industries"),("TCS","Tata Consultancy Services"),("HDFCBANK","HDFC Bank"),
        ("ICICIBANK","ICICI Bank"),("INFY","Infosys"),("SBIN","State Bank of India"),("ITC","ITC"),
        ("BHARTIARTL","Bharti Airtel"),("LT","Larsen & Toubro"),("AXISBANK","Axis Bank"),
        ("KOTAKBANK","Kotak Mahindra Bank"),("BAJFINANCE","Bajaj Finance"),("WIPRO","Wipro"),
        ("HINDUNILVR","Hindustan Unilever"),("MARUTI","Maruti Suzuki"),("TATASTEEL","Tata Steel"),
        ("NTPC","NTPC"),("POWERGRID","Power Grid"),("SUNPHARMA","Sun Pharma"),("ADANIENT","Adani Enterprises")
    ];

    public GoogleNewsRssProvider(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<NewsArticleDto>> FetchLatestAsync(CancellationToken cancellationToken = default)
    {
        var companyQueries = Universe
            .Chunk(5)
            .Select(batch => string.Join(" OR ", batch.Select(x => $"\\\"{x.Name}\\\" OR {x.Ticker}")))
            .ToArray();
        var queries = new[] { "Indian stock market", "NSE India stocks", "RBI India economy" }
            .Concat(companyQueries);
        var all = new Dictionary<string, NewsArticleDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var query in queries)
        {
            var url = "https://news.google.com/rss/search?q=" + Uri.EscapeDataString(query) + "&hl=en-IN&gl=IN&ceid=IN:en";
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) continue;
                var xml = await response.Content.ReadAsStringAsync(cancellationToken);
                foreach (var item in XDocument.Parse(xml).Descendants("item"))
                {
                    var title = item.Element("title")?.Value?.Trim();
                    var link = item.Element("link")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link)) continue;
                    var published = DateTimeOffset.TryParse(item.Element("pubDate")?.Value, out var dt) ? dt : DateTimeOffset.UtcNow;
                    var source = item.Element("source")?.Value?.Trim() ?? "Google News";
                    var cleanTitle = title.Split(" - ").FirstOrDefault()?.Trim() ?? title;
                    var matched = Universe.Where(x => ContainsCompany(title, x)).Select(x => x.Ticker).Distinct().ToArray();
                    var category = Classify(title);
                    var sentimentScore = ScoreSentiment(title);
                    var sentiment = sentimentScore >= 0.15m ? "Positive" : sentimentScore <= -0.15m ? "Negative" : "Neutral";
                    var impact = ClassifyImpact(title);
                    var relevance = matched.Length > 0 ? 0.95m : category == "Market" ? 0.8m : 0.5m;
                    var id = DeterministicId(link);
                    var article = new NewsArticleDto
                    {
                        Id=id, SourceName=source, Title=cleanTitle, Summary=title, Url=link,
                        PublishedAt=published, Category=category, Sentiment=sentiment, SentimentScore=sentimentScore,
                        Impact=impact, RelevanceScore=relevance, Tickers=matched
                    };
                    all.TryAdd(link, article);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { /* One feed failing must not prevent other feeds. */ }
        }
        return all.Values.OrderByDescending(x => x.PublishedAt).Take(250).ToArray();
    }

    private static bool ContainsCompany(string text, (string Ticker,string Name) company)
        => text.Contains(company.Ticker, StringComparison.OrdinalIgnoreCase) ||
           text.Contains(company.Name, StringComparison.OrdinalIgnoreCase);

    private static string Classify(string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("rbi") || t.Contains("inflation") || t.Contains("gdp") || t.Contains("interest rate")) return "Economy";
        if (t.Contains("earnings") || t.Contains("profit") || t.Contains("revenue") || t.Contains("result")) return "Earnings";
        if (t.Contains("merger") || t.Contains("acquisition") || t.Contains("deal") || t.Contains("order")) return "Corporate";
        if (t.Contains("global") || t.Contains("fed") || t.Contains("oil") || t.Contains("tariff")) return "Global";
        return "Market";
    }

    private static string ClassifyImpact(string title)
    {
        var t = title.ToLowerInvariant();
        var high = new[] { "results","earnings","acquisition","merger","regulatory","approval","penalty","fraud","downgrade","upgrade","guidance" };
        var medium = new[] { "order","deal","launch","investment","target","dividend","buyback" };
        return high.Any(t.Contains) ? "High" : medium.Any(t.Contains) ? "Medium" : "Low";
    }

    private static decimal ScoreSentiment(string title)
    {
        var t = title.ToLowerInvariant();
        var positive = new[] { "surge","rises","gain","gains","profit","growth","beats","strong","upgrade","bullish","record","approval","wins","positive","outperform","buyback","dividend" };
        var negative = new[] { "falls","fall","drop","drops","loss","losses","weak","misses","downgrade","bearish","fraud","penalty","probe","warning","cuts","decline","negative","underperform" };
        var p = positive.Count(t.Contains); var n = negative.Count(t.Contains);
        return Math.Clamp((p - n) * 0.25m, -1m, 1m);
    }

    private static Guid DeterministicId(string value)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes(value))[..16]);
}
