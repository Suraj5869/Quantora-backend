using MediatR;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Modules.Profile.DTOs;
using Quantora.Application.Modules.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Queries.GetProfile
{
    public sealed class GetProfileQueryHandler
    : IRequestHandler<GetProfileQuery, ProfileResponse>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetProfileQueryHandler(
            IProfileRepository profileRepository,
            ICurrentUserService currentUserService)
        {
            _profileRepository = profileRepository;
            _currentUserService = currentUserService;
        }

        public async Task<ProfileResponse> Handle(
            GetProfileQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _profileRepository.GetByIdAsync(
                _currentUserService.UserId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "User profile could not be found.");
            }

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
