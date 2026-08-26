using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Quantora.Infrastructure.Broker.Upstox.Models
{
    public sealed class UpstoxOrderDetailsItem
    {
        [JsonPropertyName("exchange")]
        public string Exchange { get; init; } = string.Empty;

        [JsonPropertyName("product")]
        public string Product { get; init; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("guid")]
        public string? Guid { get; init; }

        [JsonPropertyName("tag")]
        public string? Tag { get; init; }

        [JsonPropertyName("instrument_token")]
        public string InstrumentToken { get; init; } = string.Empty;

        [JsonPropertyName("placed_by")]
        public string? PlacedBy { get; init; }

        [JsonPropertyName("tradingsymbol")]
        public string? TradingSymbol { get; init; }

        [JsonPropertyName("trading_symbol")]
        public string? TradingSymbolAlias { get; init; }

        [JsonPropertyName("order_type")]
        public string OrderType { get; init; } = string.Empty;

        [JsonPropertyName("validity")]
        public string Validity { get; init; } = string.Empty;

        [JsonPropertyName("trigger_price")]
        public decimal TriggerPrice { get; init; }

        [JsonPropertyName("disclosed_quantity")]
        public int DisclosedQuantity { get; init; }

        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; init; } = string.Empty;

        [JsonPropertyName("average_price")]
        public decimal AveragePrice { get; init; }

        [JsonPropertyName("filled_quantity")]
        public int FilledQuantity { get; init; }

        [JsonPropertyName("pending_quantity")]
        public int PendingQuantity { get; init; }

        [JsonPropertyName("status_message")]
        public string? StatusMessage { get; init; }

        [JsonPropertyName("status_message_raw")]
        public string? StatusMessageRaw { get; init; }

        [JsonPropertyName("exchange_order_id")]
        public string? ExchangeOrderId { get; init; }

        [JsonPropertyName("parent_order_id")]
        public string? ParentOrderId { get; init; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; init; } = string.Empty;

        [JsonPropertyName("variety")]
        public string? Variety { get; init; }

        [JsonPropertyName("order_timestamp")]
        public string? OrderTimestamp { get; init; }

        [JsonPropertyName("exchange_timestamp")]
        public string? ExchangeTimestamp { get; init; }

        [JsonPropertyName("is_amo")]
        public bool IsAmo { get; init; }

        [JsonPropertyName("order_request_id")]
        public string? OrderRequestId { get; init; }

        [JsonPropertyName("order_ref_id")]
        public string? OrderRefId { get; init; }
    }
}
