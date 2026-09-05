using Microsoft.Extensions.Options;
using Quantora.Application.Configurations;
using Quantora.Application.Modules.Broker.Interfaces;

namespace Quantora.Infrastructure.Broker.Upstox
{
    public sealed class UpstoxBrokerProvider : IBrokerProvider
    {
        private readonly UpstoxSettings _settings;

        public UpstoxBrokerProvider(
            IOptions<UpstoxSettings> options)
        {
            _settings = options.Value;
        }

        public string BrokerName => "Upstox";

        public bool IsSandbox => _settings.UseSandbox;

        public Task<bool> IsConfiguredAsync(
            CancellationToken cancellationToken = default)
        {
            var configured =
                !string.IsNullOrWhiteSpace(
                    _settings.SandboxAccessToken);

            return Task.FromResult(configured);
        }

        public Task<string> GetAuthorizationUrlAsync(
         string state,
         CancellationToken cancellationToken = default)
        {
            var queryParameters = new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["redirect_uri"] = _settings.RedirectUri,
                ["response_type"] = "code",
                ["state"] = state
            };

            var queryString = string.Join(
                "&",
                queryParameters.Select(
                    parameter =>
                        $"{Uri.EscapeDataString(parameter.Key)}=" +
                        $"{Uri.EscapeDataString(parameter.Value)}"));

            var authorizationUrl =
                $"https://api.upstox.com/v2/login/authorization/dialog?{queryString}";

            return Task.FromResult(authorizationUrl);
        }
    }
}
