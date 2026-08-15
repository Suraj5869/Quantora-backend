using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.DTOs
{
    public sealed class ProfileResponse
    {
        public Guid Id { get; init; }

        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public bool IsEmailVerified { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? LastLoginAt { get; init; }
    }
}
