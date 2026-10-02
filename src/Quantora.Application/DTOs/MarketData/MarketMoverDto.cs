namespace Quantora.Application.DTOs.MarketData;

public sealed class MarketMoverDto
{
    public string Name { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public string InstrumentKey { get; init; } = string.Empty;
    public decimal LastPrice { get; init; }
    public decimal PreviousClose { get; init; }
    public decimal NetChange { get; init; }
    public decimal ChangePercent { get; init; }
    public long Volume { get; init; }
}
