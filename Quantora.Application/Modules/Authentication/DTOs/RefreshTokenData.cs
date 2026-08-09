using Quantora.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.DTOs
{
    public sealed class RefreshTokenData
    {
        public RefreshToken Token { get; init; } = new();

        public Guid UserId { get; init; }

        public string Email { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsEmailVerified { get; init; }
    }
}
