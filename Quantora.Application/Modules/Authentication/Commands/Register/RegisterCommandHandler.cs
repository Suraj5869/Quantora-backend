using MediatR;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Register
{
    public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IUserRepository _userRepository;

        public RegisterCommandHandler(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<RegisterResponse> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser =
                await _userRepository.GetByEmailAsync(
                    email,
                    cancellationToken);

            if (existingUser is not null)
            {
                throw new AuthenticationException(
                    "An account with this email already exists.");
            }

            var now = DateTimeOffset.UtcNow;

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.CreateAsync(
                user,
                cancellationToken);

            return new RegisterResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
            };
        }
    }
}
