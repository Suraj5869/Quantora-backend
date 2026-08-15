using MediatR;
using Quantora.Application.Modules.Profile.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Commands.UpdateProfile
{
    public sealed record UpdateProfileCommand(
    string FullName
) : IRequest<ProfileResponse>;
}
