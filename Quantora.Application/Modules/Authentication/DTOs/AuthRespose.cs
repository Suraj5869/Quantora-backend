using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.DTOs
{
    public sealed class AuthResponse
    {
        public string AccessToken { get; init; } = string.Empty;

        public string RefreshToken { get; init; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; init; }

        public UserResponse User { get; init; } = new();
    }
}
