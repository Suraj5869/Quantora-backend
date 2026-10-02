namespace Quantora.Application.DTOs.MarketData;

public sealed class MarketCandlesResponseDto
{
    public string Provider { get; init; } = "Upstox";
    public string InstrumentKey { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int Interval { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
    public IReadOnlyList<MarketCandleDto> Candles { get; init; } =
        Array.Empty<MarketCandleDto>();
}
