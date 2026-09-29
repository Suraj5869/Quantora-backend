using Quantora.Application.DTOs.Broker;

namespace Quantora.Application.Services;

public interface IBrokerService
{
    Task<BrokerConnectionResponse> GetConnectionAsync(
        CancellationToken cancellationToken = default);

    Task DisconnectUpstoxAsync(
    CancellationToken cancellationToken = default);

    Task<string> GetAuthorizationUrlAsync(
        CancellationToken cancellationToken = default);

    Task<UpstoxOAuthCallbackResponse> HandleOAuthCallbackAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default);

    Task<UpstoxOrderResponse> PlaceSandboxOrderAsync(
        UpstoxPlaceOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<UpstoxOrderDetailsResponse> GetOrderDetailsAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    Task<UpstoxModifyOrderResponse> ModifySandboxOrderAsync(
        UpstoxModifyOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<UpstoxCancelOrderResponse> CancelSandboxOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);
}