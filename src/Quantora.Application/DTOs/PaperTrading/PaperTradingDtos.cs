namespace Quantora.Application.DTOs.PaperTrading;

public sealed class PlacePaperOrderRequest
{
    public string InstrumentKey { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal? StopLossPrice { get; init; }
}
public sealed class PaperPositionDto
{
    public string InstrumentKey { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal LastPrice { get; init; }
    public decimal? StopLossPrice { get; init; }
    public decimal MarketValue { get; init; }
    public decimal UnrealizedPnl { get; init; }
}
public sealed class StopLossSimulationRequest
{
    public string InstrumentKey { get; init; } = string.Empty;
    public decimal SimulatedPrice { get; init; }
}
public sealed class StopLossSimulationResult
{
    public string InstrumentKey { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public decimal? StopLossPrice { get; init; }
    public decimal SimulatedPrice { get; init; }
    public bool WouldTrigger { get; init; }
    public string Message { get; init; } = string.Empty;
}
public sealed class PaperOrderDto
{
    public Guid Id { get; init; }
    public string InstrumentKey { get; init; } = string.Empty;
    public string TradingSymbol { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? ExecutionPrice { get; init; }
    public decimal? TotalValue { get; init; }
    public decimal RealizedPnl { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExecutedAt { get; init; }
}
public sealed class PaperAccountDto
{
    public Guid Id { get; init; }
    public decimal InitialCash { get; init; }
    public decimal AvailableCash { get; init; }
    public decimal InvestedValue { get; init; }
    public decimal PortfolioValue { get; init; }
    public decimal TotalPnl { get; init; }
    public IReadOnlyList<PaperPositionDto> Positions { get; init; } = Array.Empty<PaperPositionDto>();
    public IReadOnlyList<PaperOrderDto> RecentOrders { get; init; } = Array.Empty<PaperOrderDto>();
}
