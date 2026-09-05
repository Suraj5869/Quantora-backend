using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.DTOs
{
    public sealed class BrokerConnectionResponse
    {
        public string Broker { get; init; } = string.Empty;

        public bool IsConnected { get; init; }

        public bool IsSandbox { get; init; }

        public string Environment { get; init; } = string.Empty;

        public DateTimeOffset CheckedAt { get; init; }  
    }
}
