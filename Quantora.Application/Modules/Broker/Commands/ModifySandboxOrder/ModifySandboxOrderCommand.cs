using MediatR;
using Quantora.Application.Modules.Broker.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Commands.ModifySandboxOrder
{
    public sealed record ModifySandboxOrderCommand(
    UpstoxModifyOrderRequest Request)
    : IRequest<UpstoxModifyOrderResponse>;
}
