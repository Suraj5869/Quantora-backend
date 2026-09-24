namespace Quantora.Application.DTOs.Broker;
public sealed class UpstoxModifyOrderRequest
{
    public string OrderId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal TriggerPrice { get; init; }
    public string Validity { get; init; } = "DAY";
    public string OrderType { get; init; } = "LIMIT";
    public int DisclosedQuantity { get; init; }
}
