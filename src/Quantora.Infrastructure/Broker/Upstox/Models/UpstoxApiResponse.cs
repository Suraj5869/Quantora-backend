using System.Text.Json.Serialization;

namespace Quantora.Infrastructure.Broker.Upstox.Models;

public sealed class UpstoxApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("errors")]
    public List<UpstoxApiError>? Errors { get; init; }
}

public sealed class UpstoxApiError
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("propertyPath")]
    public string? PropertyPath { get; init; }

    [JsonPropertyName("invalidValue")]
    public string? InvalidValue { get; init; }
}

public sealed class UpstoxPlaceOrderData
{
    [JsonPropertyName("order_ids")]
    public List<string> OrderIds { get; init; } = [];
}

public sealed class UpstoxModifyOrderData
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; init; } = string.Empty;
}

public sealed class UpstoxCancelOrderData
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; init; } = string.Empty;
}
