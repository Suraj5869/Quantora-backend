using MediatR;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Modules.Profile.DTOs;
using Quantora.Application.Modules.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Commands.UpdateProfile
{
    public sealed class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, ProfileResponse>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly ICurrentUserService _currentUserService;

        public UpdateProfileCommandHandler(
            IProfileRepository profileRepository,
            ICurrentUserService currentUserService)
        {
            _profileRepository = profileRepository;
            _currentUserService = currentUserService;
        }

        public async Task<ProfileResponse> Handle(
            UpdateProfileCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var user = await _profileRepository.GetByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "User profile could not be found.");
            }

            var fullName = request.FullName.Trim();

            if (string.Equals(
                    user.FullName,
                    fullName,
                    StringComparison.Ordinal))
            {
                return MapToResponse(user);
            }

            var updatedAt = DateTimeOffset.UtcNow;

            var updated = await _profileRepository.UpdateFullNameAsync(
                userId,
                fullName,
                updatedAt,
                cancellationToken);

            if (!updated)
            {
                throw new InvalidOperationException(
                    "Profile could not be updated.");
            }

            user.FullName = fullName;
            user.UpdatedAt = updatedAt;

            return MapToResponse(user);
        }

        private static ProfileResponse MapToResponse(
            Quantora.Domain.Entities.User user)
        {
            return new ProfileResponse
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
    }
}
