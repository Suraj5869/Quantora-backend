using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.DTOs
{
    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
