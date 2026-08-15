using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Modules.Profile.Commands.UpdateProfile;
using Quantora.Application.Modules.Profile.DTOs;
using Quantora.Application.Modules.Profile.Queries.GetProfile;

namespace Quantora.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/profile")]
    public sealed class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(ProfileResponse),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetProfileQuery(),
                cancellationToken);

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(
            typeof(ProfileResponse),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateProfileCommand(request.FullName),
                cancellationToken);

            return Ok(result);
        }
    }
}
