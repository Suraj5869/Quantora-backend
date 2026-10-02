using System.Globalization;
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

    public UpstoxMarketDataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<MarketCandleDto>> GetHistoricalCandlesAsync(
        string accessToken,
        string instrumentKey,
        string unit,
        int interval,
        DateOnly toDate,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var key = Uri.EscapeDataString(instrumentKey);
        var url = $"{BaseUrl}/v3/historical-candle/{key}/{unit}/{interval}/" +
                  $"{toDate:yyyy-MM-dd}/{fromDate:yyyy-MM-dd}";

        return GetCandlesAsync(url, accessToken, cancellationToken);
    }

    public Task<IReadOnlyList<MarketCandleDto>> GetIntradayCandlesAsync(
        string accessToken,
        string instrumentKey,
        string unit,
        int interval,
        CancellationToken cancellationToken = default)
    {
        var key = Uri.EscapeDataString(instrumentKey);
        var url = $"{BaseUrl}/v3/historical-candle/intraday/{key}/{unit}/{interval}";

        return GetCandlesAsync(url, accessToken, cancellationToken);
    }

    private async Task<IReadOnlyList<MarketCandleDto>> GetCandlesAsync(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            throw new UpstoxApiException(
                (int)response.StatusCode,
                "Upstox returned an invalid market-data response.");
        }

        using (document)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = "Upstox market-data request failed.";
                if (document.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array &&
                    errors.GetArrayLength() > 0 &&
                    errors[0].TryGetProperty("message", out var errorMessage) &&
                    errorMessage.ValueKind == JsonValueKind.String)
                {
                    message = errorMessage.GetString() ?? message;
                }

                throw new UpstoxApiException((int)response.StatusCode, message);
            }

            if (!document.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("candles", out var candleArray) ||
                candleArray.ValueKind != JsonValueKind.Array)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Upstox response did not contain candle data.");
            }

            var candles = new List<MarketCandleDto>(candleArray.GetArrayLength());
            foreach (var item in candleArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array ||
                    item.GetArrayLength() < 6)
                {
                    continue;
                }

                var timestampText = item[0].GetString();
                if (!DateTimeOffset.TryParse(
                    timestampText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var timestamp))
                {
                    continue;
                }

                candles.Add(new MarketCandleDto
                {
                    Timestamp = timestamp,
                    Open = item[1].GetDecimal(),
                    High = item[2].GetDecimal(),
                    Low = item[3].GetDecimal(),
                    Close = item[4].GetDecimal(),
                    Volume = item[5].GetInt64(),
                    OpenInterest = item.GetArrayLength() > 6 &&
                                   item[6].ValueKind == JsonValueKind.Number
                        ? item[6].GetInt64()
                        : null
                });
            }

            return candles.OrderBy(candle => candle.Timestamp).ToArray();
        }
    }
}
