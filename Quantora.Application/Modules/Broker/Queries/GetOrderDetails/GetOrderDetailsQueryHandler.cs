using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Queries.GetOrderDetails
{
    public sealed class GetOrderDetailsQueryHandler
    : IRequestHandler<
        GetOrderDetailsQuery,
        UpstoxOrderDetailsResponse>
    {
        private readonly IUpstoxClient _upstoxClient;

        public GetOrderDetailsQueryHandler(
            IUpstoxClient upstoxClient)
        {
            _upstoxClient = upstoxClient;
        }

        public Task<UpstoxOrderDetailsResponse> Handle(
            GetOrderDetailsQuery request,
            CancellationToken cancellationToken)
        {
            return _upstoxClient.GetOrderDetailsAsync(
                request.OrderId,
                cancellationToken);
        }
    }
}
