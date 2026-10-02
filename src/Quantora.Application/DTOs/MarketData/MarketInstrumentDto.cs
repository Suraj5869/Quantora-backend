namespace Quantora.Application.DTOs.MarketData;

public sealed class MarketInstrumentDto
{
    public string Name { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public string InstrumentKey { get; init; } = string.Empty;
    public string Exchange { get; init; } = "NSE";
    public string Segment { get; init; } = "EQ";
}
