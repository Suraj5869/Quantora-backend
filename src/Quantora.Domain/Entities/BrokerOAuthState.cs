using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Domain.Entities
{
    public sealed class BrokerOAuthState
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Broker { get; set; } = string.Empty;

        public string StateHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? ConsumedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
