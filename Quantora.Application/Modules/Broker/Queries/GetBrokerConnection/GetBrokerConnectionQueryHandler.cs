using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Queries.GetBrokerConnection
{
    public sealed class GetBrokerConnectionQueryHandler
    : IRequestHandler<
        GetBrokerConnectionQuery,
        BrokerConnectionResponse>
    {
        private readonly IBrokerProvider _brokerProvider;

        public GetBrokerConnectionQueryHandler(
            IBrokerProvider brokerProvider)
        {
            _brokerProvider = brokerProvider;
        }

        public async Task<BrokerConnectionResponse> Handle(
            GetBrokerConnectionQuery request,
            CancellationToken cancellationToken)
        {
            var configured =
                await _brokerProvider.IsConfiguredAsync(
                    cancellationToken);

            return new BrokerConnectionResponse
            {
                Broker = _brokerProvider.BrokerName,
                IsConnected = configured,
                IsSandbox = _brokerProvider.IsSandbox,
                Environment = _brokerProvider.IsSandbox
                    ? "Sandbox"
                    : "Production",
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
