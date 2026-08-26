using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.DTOs
{
    public sealed class UpstoxOrderDetailsResponse
    {
        public string OrderId { get; init; } = string.Empty;

        public string InstrumentToken { get; init; } = string.Empty;

        public string Exchange { get; init; } = string.Empty;

        public string Product { get; init; } = string.Empty;

        public string TransactionType { get; init; } = string.Empty;

        public string OrderType { get; init; } = string.Empty;

        public string Validity { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public int DisclosedQuantity { get; init; }

        public decimal Price { get; init; }

        public decimal AveragePrice { get; init; }

        public string Status { get; init; } = string.Empty;

        public string? StatusMessage { get; init; }

        public string? Tag { get; init; }

        public DateTimeOffset? OrderTimestamp { get; init; }
    }
}
