using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.PlaceSandboxOrder
{
    public sealed record PlaceSandboxOrderCommand(
    UpstoxPlaceOrderRequest Request)
    : IRequest<UpstoxOrderResponse>;
}
