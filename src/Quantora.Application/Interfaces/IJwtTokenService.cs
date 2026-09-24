namespace Quantora.Application.Interfaces;
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
