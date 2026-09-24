using Quantora.Application.DTOs.Auth;
namespace Quantora.Application.Services;
public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginContext context, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenContext context, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
public sealed record LoginContext(LoginRequest Request, string? IpAddress);
public sealed record RefreshTokenContext(string RefreshToken, string? IpAddress);
