using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Services
{
    public static class TokenHashService
    {
        public static string Hash(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes)
                .ToLowerInvariant();
        }
    }
}
