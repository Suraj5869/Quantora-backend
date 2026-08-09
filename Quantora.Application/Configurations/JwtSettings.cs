namespace Quantora.Application.Configurations
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; init; } = 30;

        public int RefreshTokenExpiryDays { get; init; } = 7;
    }
}