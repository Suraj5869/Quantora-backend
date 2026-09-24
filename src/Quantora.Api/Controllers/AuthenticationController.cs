using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Common;
using Quantora.Application.DTOs.Auth;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthenticationController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _authService.RegisterAsync(
                request,
                cancellationToken);

        return Ok(
            ApiResponse<RegisterResponse>.SuccessResponse(
                result,
                "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _authService.LoginAsync(
                new LoginContext(
                    request,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

        return Ok(
            ApiResponse<AuthResponse>.SuccessResponse(
                result,
                "Login successful."));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(
            request.RefreshToken,
            cancellationToken);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null!,
                "Logout successful."));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _authService.RefreshTokenAsync(
                new RefreshTokenContext(
                    request.RefreshToken,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

        return Ok(
            ApiResponse<AuthResponse>.SuccessResponse(
                result,
                "Token refreshed successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var result =
            await _authService.GetCurrentUserAsync(
                cancellationToken);

        return Ok(
            ApiResponse<UserResponse>.SuccessResponse(
                result,
                "Current user retrieved successfully."));
    }
}
