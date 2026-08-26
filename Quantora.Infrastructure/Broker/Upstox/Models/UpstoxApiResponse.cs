using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Quantora.Infrastructure.Broker.Upstox.Models
{
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

    public sealed class UpstoxOrderHistoryItem
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; init; } = string.Empty;

        [JsonPropertyName("instrument_token")]
        public string InstrumentToken { get; init; } = string.Empty;

        [JsonPropertyName("exchange")]
        public string Exchange { get; init; } = string.Empty;

        [JsonPropertyName("product")]
        public string Product { get; init; } = string.Empty;

        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; init; } = string.Empty;

        [JsonPropertyName("order_type")]
        public string OrderType { get; init; } = string.Empty;

        [JsonPropertyName("validity")]
        public string Validity { get; init; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("disclosed_quantity")]
        public int DisclosedQuantity { get; init; }

        [JsonPropertyName("price")]
        public decimal Price { get; init; }

        [JsonPropertyName("average_price")]
        public decimal AveragePrice { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("status_message")]
        public string? StatusMessage { get; init; }

        [JsonPropertyName("tag")]
        public string? Tag { get; init; }

        [JsonPropertyName("order_timestamp")]
        public string? OrderTimestamp { get; init; }
    }

    public sealed class UpstoxModifyOrderData
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; init; } = string.Empty;
    }
}
