using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Quantora.Application.DTOs.Broker
{
    public sealed class UpstoxOAuthTokenResponse
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("exchanges")]
        public string[] Exchanges { get; init; } = [];

        [JsonPropertyName("products")]
        public string[] Products { get; init; } = [];

        [JsonPropertyName("broker")]
        public string Broker { get; init; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; init; } = string.Empty;

        [JsonPropertyName("user_name")]
        public string UserName { get; init; } = string.Empty;

        [JsonPropertyName("order_types")]
        public string[] OrderTypes { get; init; } = [];

        [JsonPropertyName("user_type")]
        public string UserType { get; init; } = string.Empty;

        [JsonPropertyName("poa")]
        public bool Poa { get; init; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; init; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("extended_token")]
        public string? ExtendedToken { get; init; }
    }
}
