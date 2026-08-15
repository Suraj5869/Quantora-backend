using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.DTOs
{
    public sealed class UpdateProfileRequest
    {
        public string FullName { get; init; } = string.Empty;
    }
}