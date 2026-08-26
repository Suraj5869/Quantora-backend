using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Quantora.Infrastructure.Broker.Upstox.Models
{
    public sealed class UpstoxCancelOrderData
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; init; } = string.Empty;
    }
}
