using Quantora.Application.Common.Interfaces;
using Quantora.Application.Configurations;
using Quantora.Application.DTOs.Auth;
using Quantora.Application.Exceptions;
using Quantora.Application.Interfaces;
using Microsoft.Extensions.Options;
using Quantora.Domain.Entities;
using BCrypt.Net;

namespace Quantora.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = NormalizeEmail(request.Email);

        if (fullName.Length < 2 || fullName.Length > 150)
            throw new ArgumentException("Full name must contain 2 to 150 characters.");

        if (email.Length > 320 || !email.Contains('@'))
            throw new ArgumentException("A valid email is required.");

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8 ||
            request.Password.Length > 128)
            throw new ArgumentException("Password must be between 8 and 128 characters.");

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.CreateAsync(user, cancellationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(
        LoginContext context,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(context.Request.Email);

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null ||
            !BCrypt.Net.BCrypt.Verify(
                context.Request.Password,
                user.PasswordHash))
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (!user.IsActive)
            throw new AuthenticationException("This account is inactive.");

        var now = DateTimeOffset.UtcNow;

        await _userRepository.UpdateLastLoginAsync(
            user.Id,
            now,
            cancellationToken);

        var accessToken =
            _jwtTokenService.GenerateAccessToken(user.Id, user.Email);

        var refreshToken =
            _jwtTokenService.GenerateRefreshToken();

        await _refreshTokenRepository.CreateAsync(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = _jwtTokenService.HashRefreshToken(refreshToken),
                CreatedAt = now,
                ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                CreatedByIp = context.IpAddress
            },
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            User = MapUser(user)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenContext context,
        CancellationToken cancellationToken = default)
    {
        var tokenHash =
            _jwtTokenService.HashRefreshToken(context.RefreshToken);

        var tokenData =
            await _refreshTokenRepository.GetWithUserAsync(
                tokenHash,
                cancellationToken);

        if (tokenData is null)
            throw new AuthenticationException("Invalid refresh token.");

        if (tokenData.Token.RevokedAt is not null)
            throw new AuthenticationException("Refresh token has been revoked.");

        if (tokenData.Token.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new AuthenticationException("Refresh token has expired.");

        if (!tokenData.IsActive)
            throw new AuthenticationException("User account is inactive.");

        var now = DateTimeOffset.UtcNow;

        var accessToken =
            _jwtTokenService.GenerateAccessToken(
                tokenData.UserId,
                tokenData.Email);

        var newRefreshToken =
            _jwtTokenService.GenerateRefreshToken();

        var newEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = tokenData.UserId,
            TokenHash =
                _jwtTokenService.HashRefreshToken(newRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedByIp = context.IpAddress
        };

        await _refreshTokenRepository.CreateAsync(
            newEntity,
            cancellationToken);

        await _refreshTokenRepository.RevokeAsync(
            tokenData.Token.Id,
            newEntity.Id,
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            User = new UserResponse
            {
                Id = tokenData.UserId,
                FullName = tokenData.FullName,
                Email = tokenData.Email,
                IsEmailVerified = tokenData.IsEmailVerified
            }
        };
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash =
            _jwtTokenService.HashRefreshToken(refreshToken);

        var token =
            await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (token is null || token.RevokedAt is not null)
            return;

        await _refreshTokenRepository.RevokeAsync(
            token.Id,
            cancellationToken: cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var user =
            await _userRepository.GetByIdAsync(
                _currentUserService.UserId,
                cancellationToken);

        if (user is null)
            throw new AuthenticationException(
                "User account could not be found.");

        if (!user.IsActive)
            throw new AuthenticationException(
                "User account is inactive.");

        return MapUser(user);
    }

    private static UserResponse MapUser(User user) =>
        new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified
        };

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
