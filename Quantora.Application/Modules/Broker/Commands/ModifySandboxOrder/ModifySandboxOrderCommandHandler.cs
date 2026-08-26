using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.ModifySandboxOrder
{
    public sealed class ModifySandboxOrderCommandHandler
     : IRequestHandler<
         ModifySandboxOrderCommand,
         UpstoxModifyOrderResponse>
    {
        private readonly IUpstoxClient _upstoxClient;

        public ModifySandboxOrderCommandHandler(
            IUpstoxClient upstoxClient)
        {
            _upstoxClient = upstoxClient;
        }

        public Task<UpstoxModifyOrderResponse> Handle(
            ModifySandboxOrderCommand request,
            CancellationToken cancellationToken)
        {
            return _upstoxClient.ModifySandboxOrderAsync(
                request.Request,
                cancellationToken);
        }
    }
}
