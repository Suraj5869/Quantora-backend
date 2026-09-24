using Microsoft.Extensions.Options;
using Quantora.Application.Configurations;
using Quantora.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Quantora.Infrastructure.Security
{
    public sealed class AesSecretProtector : ISecretProtector
    {
        private readonly byte[] _key;

        public AesSecretProtector(
            IOptions<SecuritySettings> options)
        {
            if (string.IsNullOrWhiteSpace(
                options.Value.EncryptionKey))
            {
                throw new InvalidOperationException(
                    "Security:EncryptionKey is not configured.");
            }

            try
            {
                _key = Convert.FromBase64String(
                    options.Value.EncryptionKey);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "Security:EncryptionKey must be a Base64 encoded 32-byte key.");
            }

            if (_key.Length != 32)
            {
                throw new InvalidOperationException(
                    "Security:EncryptionKey must decode to exactly 32 bytes.");
            }
        }

        public string Protect(string value)
        {
            var plaintext =
                Encoding.UTF8.GetBytes(value);

            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(_key, 16);

            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag);

            return string.Join(
                ".",
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag));
        }

        public string Unprotect(string protectedValue)
        {
            var parts = protectedValue.Split('.');

            if (parts.Length != 3)
            {
                throw new CryptographicException(
                    "Invalid protected value.");
            }

            var nonce = Convert.FromBase64String(parts[0]);
            var ciphertext = Convert.FromBase64String(parts[1]);
            var tag = Convert.FromBase64String(parts[2]);

            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_key, 16);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
