using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Domain.Entities
{
    public sealed class BrokerConnection
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Broker { get; set; } = string.Empty;

        public string BrokerUserId { get; set; } = string.Empty;

        public string AccessTokenEncrypted { get; set; } = string.Empty;

        public string? ExtendedTokenEncrypted { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset ConnectedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
