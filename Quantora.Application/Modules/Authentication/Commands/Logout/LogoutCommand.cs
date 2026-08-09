using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Logout
{
    public sealed record LogoutCommand(
    string RefreshToken
) : IRequest;
}
