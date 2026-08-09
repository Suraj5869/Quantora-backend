using MediatR;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Application.Modules.Authentication.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Queries.GetCurrentUser
{
    public sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, UserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetCurrentUserQueryHandler(
            IUserRepository userRepository,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _currentUserService = currentUserService;
        }

        public async Task<UserResponse> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            var user =
                await _userRepository.GetByIdAsync(
                    _currentUserService.UserId,
                    cancellationToken);

            if (user is null)
            {
                throw new AuthenticationException(
                    "User account could not be found.");
            }

            if (!user.IsActive)
            {
                throw new AuthenticationException(
                    "User account is inactive.");
            }

            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified
            };
        }
    }
}
