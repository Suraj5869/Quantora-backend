using Quantora.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Quantora.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?
            .User
            .Identity?
            .IsAuthenticated
        ?? false;

    public Guid UserId
    {
        get
        {
            var value =
                _httpContextAccessor.HttpContext?
                    .User
                    .FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?
                    .User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException(
                    "Authenticated user ID is missing.");

            return userId;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.Email);
}
