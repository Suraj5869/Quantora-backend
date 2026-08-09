using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Application.Modules.Authentication.Services;
using System;
using System.Collections.Generic;
using System.Text;
using RefreshTokenEntity =
    Quantora.Domain.Entities.RefreshToken;

namespace Quantora.Application.Modules.Authentication.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenService _jwtTokenService;

        public RefreshTokenCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IJwtTokenService jwtTokenService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponse> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            var tokenHash =
                TokenHashService.Hash(
                    request.RefreshToken);

            var tokenData =
                await _refreshTokenRepository.GetWithUserAsync(
                    tokenHash,
                    cancellationToken);

            if (tokenData is null)
            {
                throw new AuthenticationException(
                    "Invalid refresh token.");
            }

            var storedToken = tokenData.Token;

            if (storedToken.RevokedAt is not null)
            {
                throw new AuthenticationException(
                    "Refresh token has been revoked.");
            }

            if (storedToken.ExpiresAt <=
                DateTimeOffset.UtcNow)
            {
                throw new AuthenticationException(
                    "Refresh token has expired.");
            }

            if (!tokenData.IsActive)
            {
                throw new AuthenticationException(
                    "User account is inactive.");
            }

            var now = DateTimeOffset.UtcNow;

            var accessToken =
                _jwtTokenService.GenerateAccessToken(
                    tokenData.UserId,
                    tokenData.Email);

            var newRefreshToken =
                _jwtTokenService.GenerateRefreshToken();

            var newRefreshTokenEntity =
                new RefreshTokenEntity
                {
                    Id = Guid.NewGuid(),

                    UserId = tokenData.UserId,

                    TokenHash =
                        TokenHashService.Hash(
                            newRefreshToken),

                    CreatedAt = now,

                    ExpiresAt =
                        now.AddDays(7),

                    CreatedByIp =
                        request.IpAddress
                };

            await _refreshTokenRepository.CreateAsync(
                newRefreshTokenEntity,
                cancellationToken);

            await _refreshTokenRepository.RevokeAsync(
                storedToken.Id,
                newRefreshTokenEntity.Id,
                cancellationToken);

            return new AuthResponse
            {
                AccessToken = accessToken,

                RefreshToken = newRefreshToken,

                ExpiresAt =
                    now.AddMinutes(30),

                User = new UserResponse
                {
                    Id = tokenData.UserId,
                    FullName = tokenData.FullName,
                    Email = tokenData.Email,
                    IsEmailVerified =
                        tokenData.IsEmailVerified
                }
            };
        }
    }
}
