using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Domain.Entities;

namespace Quantora.Application.Modules.Authentication.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task CreateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            Guid tokenId,
            Guid? replacedByTokenId = null,
            CancellationToken cancellationToken = default);

        Task<RefreshTokenData?> GetWithUserAsync(
    string tokenHash,
    CancellationToken cancellationToken = default);
    }
}
