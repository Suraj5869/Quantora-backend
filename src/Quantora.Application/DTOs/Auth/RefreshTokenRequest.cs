namespace Quantora.Application.DTOs.Auth;
public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
