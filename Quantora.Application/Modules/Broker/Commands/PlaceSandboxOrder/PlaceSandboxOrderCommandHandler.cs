using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.PlaceSandboxOrder
{
    public sealed class PlaceSandboxOrderCommandHandler
     : IRequestHandler<
         PlaceSandboxOrderCommand,
         UpstoxOrderResponse>
    {
        private readonly IUpstoxClient _upstoxClient;

        public PlaceSandboxOrderCommandHandler(
            IUpstoxClient upstoxClient)
        {
            _upstoxClient = upstoxClient;
        }

        public Task<UpstoxOrderResponse> Handle(
            PlaceSandboxOrderCommand request,
            CancellationToken cancellationToken)
        {
            return _upstoxClient.PlaceSandboxOrderAsync(
                request.Request,
                cancellationToken);
        }
    }
}
