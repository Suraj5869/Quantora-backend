using Quantora.Application.DTOs.Broker;

namespace Quantora.Application.Interfaces;

public interface IUpstoxClient
{
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

    Task<UpstoxOAuthTokenResponse> ExchangeAuthorizationCodeAsync(
    string code,
    CancellationToken cancellationToken = default);
}