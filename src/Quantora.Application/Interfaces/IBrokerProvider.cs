namespace Quantora.Application.Interfaces;
public interface IBrokerProvider
{
    string BrokerName { get; }
    bool IsSandbox { get; }
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<string> GetAuthorizationUrlAsync(string state, CancellationToken cancellationToken = default);
}
