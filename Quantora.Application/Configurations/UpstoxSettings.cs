namespace Quantora.Application.Configurations
{
    public sealed class UpstoxSettings
    {
        public const string SectionName = "Upstox";

        public string ClientId { get; init; } = string.Empty;

        public string ClientSecret { get; init; } = string.Empty;

        public string RedirectUri { get; init; } = string.Empty;

        public string SandboxAccessToken { get; init; } = string.Empty;

        public bool UseSandbox { get; init; } = true;
    }
}
