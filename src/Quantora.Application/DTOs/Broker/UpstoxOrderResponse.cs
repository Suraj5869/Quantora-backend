namespace Quantora.Application.DTOs.Broker;
public sealed class UpstoxOrderResponse
{
    public string OrderId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
