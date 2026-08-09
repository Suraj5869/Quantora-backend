using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.DTOs
{
    public sealed class LogoutRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
