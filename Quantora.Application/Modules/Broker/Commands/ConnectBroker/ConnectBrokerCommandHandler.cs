using MediatR;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.ConnectBroker
{
    public sealed class ConnectBrokerCommandHandler
    : IRequestHandler<ConnectBrokerCommand, string>
    {
        private readonly IBrokerProvider _brokerProvider;
        private readonly ICurrentUserService _currentUserService;

        public ConnectBrokerCommandHandler(
            IBrokerProvider brokerProvider,
            ICurrentUserService currentUserService)
        {
            _brokerProvider = brokerProvider;
            _currentUserService = currentUserService;
        }

        public async Task<string> Handle(
            ConnectBrokerCommand request,
            CancellationToken cancellationToken)
        {
            var state = _currentUserService.UserId.ToString();

            return await _brokerProvider.GetAuthorizationUrlAsync(
                state,
                cancellationToken);
        }
    }
}
