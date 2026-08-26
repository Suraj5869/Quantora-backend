using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.DTOs
{
    public sealed class UpstoxOrderResponse
    {
        public string OrderId { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;
    }
}
