using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Modules.Authentication.Commands.Login;
using Quantora.Application.Modules.Authentication.Commands.Logout;
using Quantora.Application.Modules.Authentication.Commands.RefreshToken;
using Quantora.Application.Modules.Authentication.Commands.Register;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Application.Modules.Authentication.Queries.GetCurrentUser;
using Quantora.Shared.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Quantora.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public sealed class AuthenticationController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUserService _currentUserService;

        public AuthenticationController(ISender sender, ICurrentUserService currentUserService)
        {
            _sender = sender;
            _currentUserService = currentUserService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var command = new RegisterCommand(
                request.FullName,
                request.Email,
                request.Password);

            var result = await _sender.Send(
                command,
                cancellationToken);

            return Ok(ApiResponse<RegisterResponse>.SuccessResponse(result, "Registration successful."));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var command = new LoginCommand(
                request.Email,
                request.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var result = await _sender.Send(
                command,
                cancellationToken);

            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, "Login successful."));
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            var command =
                new LogoutCommand(
                    request.RefreshToken);

            await _sender.Send(
                command,
                cancellationToken);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    null,
                    "Logout successful."));
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var command = new RefreshTokenCommand(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var result = await _sender.Send(
                command,
                cancellationToken);

            return Ok(
                ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Token refreshed successfully."));
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var result =
                await _sender.Send(
                    new GetCurrentUserQuery(),
                    cancellationToken);

            return Ok(
                ApiResponse<UserResponse>.SuccessResponse(
                    result,
                    "Current user retrieved successfully."));
        }
    }
}
