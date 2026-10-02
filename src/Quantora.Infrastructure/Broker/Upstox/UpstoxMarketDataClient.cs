using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Quantora.Application.DTOs.MarketData;
using Quantora.Application.Interfaces;
using Quantora.Infrastructure.Broker.Upstox.Exceptions;

namespace Quantora.Infrastructure.Broker.Upstox;

public sealed class UpstoxMarketDataClient : IUpstoxMarketDataClient
{
    private const string BaseUrl = "https://api.upstox.com";
    private readonly HttpClient _httpClient;

    public UpstoxMarketDataClient(HttpClient httpClient) => _httpClient = httpClient;

    public Task<IReadOnlyList<MarketCandleDto>> GetHistoricalCandlesAsync(
        string accessToken, string instrumentKey, string unit, int interval,
        DateOnly toDate, DateOnly fromDate, CancellationToken cancellationToken = default)
    {
        var key = Uri.EscapeDataString(instrumentKey);
        var url = BaseUrl + "/v3/historical-candle/" + key + "/" + unit + "/" + interval +
                  "/" + toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                  "/" + fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return GetCandlesAsync(url, accessToken, cancellationToken);
    }

    public Task<IReadOnlyList<MarketCandleDto>> GetIntradayCandlesAsync(
        string accessToken, string instrumentKey, string unit, int interval,
        CancellationToken cancellationToken = default)
    {
        var url = BaseUrl + "/v3/historical-candle/intraday/" +
                  Uri.EscapeDataString(instrumentKey) + "/" + unit + "/" + interval;
        return GetCandlesAsync(url, accessToken, cancellationToken);
    }

    public async Task<IReadOnlyList<MarketInstrumentDto>> SearchInstrumentsAsync(
        string accessToken, string query, CancellationToken cancellationToken = default)
    {
        var url = BaseUrl + "/v2/instruments/search?query=" + Uri.EscapeDataString(query) +
                  "&exchanges=NSE&segments=EQ&page_number=1&records=20";
        using var document = await GetJsonAsync(url, accessToken, cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
            return Array.Empty<MarketInstrumentDto>();

        var results = new List<MarketInstrumentDto>();
        foreach (var item in data.EnumerateArray())
        {
            var key = ReadString(item, "instrument_key");
            if (string.IsNullOrWhiteSpace(key)) continue;
            results.Add(new MarketInstrumentDto
            {
                Name = ReadString(item, "name") ?? ReadString(item, "short_name") ?? key,
                TradingSymbol = ReadString(item, "trading_symbol") ?? string.Empty,
                InstrumentKey = key,
                Exchange = ReadString(item, "exchange") ?? "NSE",
                Segment = ReadString(item, "segment") ?? "EQ"
            });
        }
        return results;
    }

    public async Task<IReadOnlyList<MarketMoverDto>> GetQuotesAsync(
        string accessToken, IReadOnlyList<MarketInstrumentDto> instruments,
        CancellationToken cancellationToken = default)
    {
        if (instruments.Count == 0) return Array.Empty<MarketMoverDto>();
        var keys = string.Join(",", instruments.Select(x => x.InstrumentKey));
        var url = BaseUrl + "/v3/market-quote/quotes?instrument_key=" + Uri.EscapeDataString(keys);
        using var document = await GetJsonAsync(url, accessToken, cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object)
            return Array.Empty<MarketMoverDto>();

        var byKey = instruments.ToDictionary(x => x.InstrumentKey, StringComparer.OrdinalIgnoreCase);
        var quotes = new List<MarketMoverDto>();
        foreach (var property in data.EnumerateObject())
        {
            var item = property.Value;
            var key = ReadString(item, "instrument_token");
            if (string.IsNullOrWhiteSpace(key) || !byKey.TryGetValue(key, out var instrument))
                continue;

            var last = ReadDecimal(item, "last_price");
            var previous = ReadDecimal(item, "prev_close_price");
            if (previous <= 0 && item.TryGetProperty("ohlc", out var ohlc))
                previous = ReadDecimal(ohlc, "close");
            var change = previous > 0 ? last - previous : ReadDecimal(item, "net_change");
            var percent = previous > 0 ? change / previous * 100m : 0m;

            quotes.Add(new MarketMoverDto
            {
                Name = instrument.Name,
                TradingSymbol = instrument.TradingSymbol,
                InstrumentKey = instrument.InstrumentKey,
                LastPrice = last,
                PreviousClose = previous,
                NetChange = change,
                ChangePercent = percent,
                Volume = ReadLong(item, "volume")
            });
        }
        return quotes;
    }

    private async Task<IReadOnlyList<MarketCandleDto>> GetCandlesAsync(
        string url, string accessToken, CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(url, accessToken, cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("candles", out var candleArray) ||
            candleArray.ValueKind != JsonValueKind.Array)
            throw new UpstoxApiException(502, "Upstox response did not contain candle data.");

        var candles = new List<MarketCandleDto>(candleArray.GetArrayLength());
        foreach (var item in candleArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 6) continue;
            if (!DateTimeOffset.TryParse(item[0].GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var timestamp)) continue;
            candles.Add(new MarketCandleDto
            {
                Timestamp = timestamp,
                Open = item[1].GetDecimal(), High = item[2].GetDecimal(),
                Low = item[3].GetDecimal(), Close = item[4].GetDecimal(),
                Volume = item[5].GetInt64(),
                OpenInterest = item.GetArrayLength() > 6 && item[6].ValueKind == JsonValueKind.Number
                    ? item[6].GetInt64() : null
            });
        }
        return candles.OrderBy(x => x.Timestamp).ToArray();
    }

    private async Task<JsonDocument> GetJsonAsync(
        string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            throw new UpstoxApiException(502, "Upstox returned an invalid market-data response.");
        }

        if (!response.IsSuccessStatusCode)
        {
            using (document)
            {
                var message = "Upstox market-data request failed.";
                if (document.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0 &&
                    errors[0].TryGetProperty("message", out var errorMessage))
                    message = errorMessage.GetString() ?? message;
                var status = response.StatusCode == HttpStatusCode.Unauthorized
                    ? 502 : (int)response.StatusCode;
                throw new UpstoxApiException(status, message);
            }
        }
        return document;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static decimal ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetDecimal(out var number) ? number : 0m;

    private static long ReadLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt64(out var number) ? number : 0L;
}
