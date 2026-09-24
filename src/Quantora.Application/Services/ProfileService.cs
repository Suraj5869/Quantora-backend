using Quantora.Application.Common.Interfaces;
using Quantora.Application.DTOs.Profile;
using Quantora.Application.Exceptions;
using Quantora.Application.Interfaces;

namespace Quantora.Application.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;

    public ProfileService(
        IProfileRepository profileRepository,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ProfileResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await _profileRepository.GetByIdAsync(
            _currentUserService.UserId,
            cancellationToken);

        if (user is null)
            throw new NotFoundException(
                "User profile could not be found.");

        return Map(user);
    }

    public async Task<ProfileResponse> UpdateAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _profileRepository.GetByIdAsync(
            _currentUserService.UserId,
            cancellationToken);

        if (user is null)
            throw new NotFoundException(
                "User profile could not be found.");

        var fullName = request.FullName.Trim();

        if (fullName.Length < 2 || fullName.Length > 150)
            throw new ArgumentException(
                "Full name must contain 2 to 150 characters.");

        if (string.Equals(
            user.FullName,
            fullName,
            StringComparison.Ordinal))
        {
            return Map(user);
        }

        var updatedAt = DateTimeOffset.UtcNow;

        var updated =
            await _profileRepository.UpdateFullNameAsync(
                user.Id,
                fullName,
                updatedAt,
                cancellationToken);

        if (!updated)
            throw new InvalidOperationException(
                "Profile could not be updated.");

        user.FullName = fullName;
        user.UpdatedAt = updatedAt;

        return Map(user);
    }

    private static ProfileResponse Map(
        Quantora.Domain.Entities.User user) =>
        new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
}
