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
    }
}
