using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.DTOs
{
    public sealed class UpstoxPlaceOrderRequest
    {
        public int Quantity { get; init; }

        public string Product { get; init; } = "D";

        public string Validity { get; init; } = "DAY";

        public decimal Price { get; init; }

        public string InstrumentToken { get; init; } = string.Empty;

        public string OrderType { get; init; } = "LIMIT";

        public string TransactionType { get; init; } = "BUY";

        public int DisclosedQuantity { get; init; }

        public decimal TriggerPrice { get; init; }

        public bool IsAmo { get; init; }

        public bool Slice { get; init; }

        public string? Tag { get; init; }
    }
}
