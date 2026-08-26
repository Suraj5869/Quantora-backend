using Quantora.Application.Modules.Broker.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Interfaces
{
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
    }
}
