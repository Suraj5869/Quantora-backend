using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Register
{
    public sealed record RegisterCommand(
     string FullName,
     string Email,
     string Password
 ) : IRequest<RegisterResponse>;
}
