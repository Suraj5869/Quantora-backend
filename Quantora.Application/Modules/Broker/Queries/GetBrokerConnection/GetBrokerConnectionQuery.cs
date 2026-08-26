using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Queries.GetBrokerConnection
{
    public sealed record GetBrokerConnectionQuery
    : IRequest<BrokerConnectionResponse>;
}
