using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Configurations
{
    public sealed class SecuritySettings
    {
        public const string SectionName = "Security";

        public string EncryptionKey { get; init; } = string.Empty;
    }
}
