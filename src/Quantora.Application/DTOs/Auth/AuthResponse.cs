namespace Quantora.Application.DTOs.Auth;
public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public UserResponse User { get; init; } = new();
}
