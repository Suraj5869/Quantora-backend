namespace Quantora.Application.DTOs.MarketData;

public sealed class MarketCandleDto
{
    public DateTimeOffset Timestamp { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
    public long? OpenInterest { get; init; }
}
