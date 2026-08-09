using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(
     string RefreshToken,
     string? IpAddress
 ) : IRequest<AuthResponse>;
}
