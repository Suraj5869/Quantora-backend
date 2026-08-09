using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Application.Modules.Authentication.Services;
using Quantora.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using RefreshTokenEntity =
    Quantora.Domain.Entities.RefreshToken;

namespace Quantora.Application.Modules.Authentication.Commands.Login
{
    public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IJwtTokenService jwtTokenService, 
            IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AuthResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email
                .Trim()
                .ToLowerInvariant();

            var user =
                await _userRepository.GetByEmailAsync(
                    email,
                    cancellationToken);

            if (user is null)
            {
                throw new AuthenticationException(
                    "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new AuthenticationException(
                    "This account is inactive.");
            }

            var passwordValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                throw new AuthenticationException(
                    "Invalid email or password.");
            }

            var now = DateTimeOffset.UtcNow;

            await _userRepository.UpdateLastLoginAsync(
                user.Id,
                now,
                cancellationToken);

            var accessToken =
                _jwtTokenService.GenerateAccessToken(
                    user.Id,
                    user.Email);

            var refreshToken =
                _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),

                UserId = user.Id,

                TokenHash =
        TokenHashService.Hash(refreshToken),

                CreatedAt = now,

                ExpiresAt =
        now.AddDays(7),

                CreatedByIp =
        request.IpAddress
            };

            await _refreshTokenRepository.CreateAsync(
                refreshTokenEntity,
                cancellationToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = now.AddMinutes(1),

                User = new UserResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsEmailVerified =
                        user.IsEmailVerified
                }
            };
        }
    }
}
