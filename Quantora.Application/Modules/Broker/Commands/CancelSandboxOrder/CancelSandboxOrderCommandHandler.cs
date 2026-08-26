using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.CancelSandboxOrder
{
    public sealed class CancelSandboxOrderCommandHandler
    : IRequestHandler<
        CancelSandboxOrderCommand,
        UpstoxCancelOrderResponse>
    {
        private readonly IUpstoxClient _upstoxClient;

        public CancelSandboxOrderCommandHandler(
            IUpstoxClient upstoxClient)
        {
            _upstoxClient = upstoxClient;
        }

        public Task<UpstoxCancelOrderResponse> Handle(
            CancelSandboxOrderCommand request,
            CancellationToken cancellationToken)
        {
            return _upstoxClient.CancelSandboxOrderAsync(
                request.OrderId,
                cancellationToken);
        }
    }
}
