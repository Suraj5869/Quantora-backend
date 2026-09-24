using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.DTOs.Broker
{
    public sealed class UpstoxOAuthCallbackResponse
    {
        public bool Success { get; init; }

        public string Broker { get; init; } = string.Empty;

        public string UpstoxUserId { get; init; } = string.Empty;

        public string UserName { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;
    }
}
