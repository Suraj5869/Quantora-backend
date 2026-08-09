using MediatR;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Application.Modules.Authentication.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Logout
{
    public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public LogoutCommandHandler(
            IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            var tokenHash =
                TokenHashService.Hash(
                    request.RefreshToken);

            var refreshToken =
                await _refreshTokenRepository.GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

            // Logout should be idempotent.
            //
            // If the token doesn't exist or was already revoked,
            // there is nothing more to do.
            if (refreshToken is null ||
                refreshToken.RevokedAt is not null)
            {
                return;
            }

            await _refreshTokenRepository.RevokeAsync(
                refreshToken.Id,
                cancellationToken: cancellationToken);
        }
    }
}
