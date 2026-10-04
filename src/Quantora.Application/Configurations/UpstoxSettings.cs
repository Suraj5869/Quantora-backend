namespace Quantora.Application.Configurations;

public sealed class UpstoxSettings
{
    public const string SectionName = "Upstox";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string SandboxAccessToken { get; init; } = string.Empty;
    // Long-lived, read-only token for market-data APIs. Never use for order operations.
    public string AnalyticsToken { get; init; } = string.Empty;
    public bool UseSandbox { get; init; } = true;
}
