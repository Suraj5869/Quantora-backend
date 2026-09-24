using Microsoft.Extensions.Options;
using Quantora.Application.Configurations;
using Quantora.Application.Interfaces;

namespace Quantora.Infrastructure.Broker.Upstox;

public sealed class UpstoxBrokerProvider : IBrokerProvider
{
    private readonly UpstoxSettings _settings;

    public UpstoxBrokerProvider(IOptions<UpstoxSettings> options)
    {
        _settings = options.Value;
    }

    public string BrokerName => "Upstox";

    public bool IsSandbox => _settings.UseSandbox;

    public Task<bool> IsConfiguredAsync(
        CancellationToken cancellationToken = default)
    {
        var configured =
            _settings.UseSandbox
                ? !string.IsNullOrWhiteSpace(_settings.SandboxAccessToken)
                : !string.IsNullOrWhiteSpace(_settings.ClientId);

        return Task.FromResult(configured);
    }

    public Task<string> GetAuthorizationUrlAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state
        };

        var query = string.Join(
            "&",
            parameters.Select(
                x =>
                    $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

        return Task.FromResult(
            $"https://api.upstox.com/v2/login/authorization/dialog?{query}");
    }
}
