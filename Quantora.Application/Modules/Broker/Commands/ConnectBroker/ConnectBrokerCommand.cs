using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.ConnectBroker
{
    public sealed record ConnectBrokerCommand
    : IRequest<string>;
}
