using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Login
{
    public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress
) : IRequest<AuthResponse>;
}
