using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.CancelSandboxOrder
{
    public sealed record CancelSandboxOrderCommand(
    string OrderId)
    : IRequest<UpstoxCancelOrderResponse>;
}
