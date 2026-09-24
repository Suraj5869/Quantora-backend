using Quantora.Domain.Entities;
namespace Quantora.Application.Interfaces;
public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<RefreshTokenUserData?> GetWithUserAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid tokenId, Guid? replacedByTokenId = null, CancellationToken cancellationToken = default);
}
public sealed class RefreshTokenUserData
{
    public RefreshToken Token { get; init; } = new();
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
}
