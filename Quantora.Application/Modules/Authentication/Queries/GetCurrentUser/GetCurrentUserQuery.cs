using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Queries.GetCurrentUser
{
    public sealed record GetCurrentUserQuery: IRequest<UserResponse>;
}
