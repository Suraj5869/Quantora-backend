using MediatR;
using Quantora.Application.Modules.Profile.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Queries.GetProfile
{
    public sealed record GetProfileQuery
     : IRequest<ProfileResponse>;
}
