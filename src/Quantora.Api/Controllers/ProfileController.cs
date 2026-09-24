using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.DTOs.Profile;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _profileService.GetAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(
            await _profileService.UpdateAsync(
                request,
                cancellationToken));
    }
}
