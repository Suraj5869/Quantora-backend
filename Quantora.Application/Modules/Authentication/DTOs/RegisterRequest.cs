using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.DTOs
{
    public sealed class RegisterRequest
    {
        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
