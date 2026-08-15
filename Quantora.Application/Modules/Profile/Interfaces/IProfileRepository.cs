using Quantora.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Interfaces
{
    public interface IProfileRepository
    {
        Task<User?> GetByIdAsync(
       Guid userId,
       CancellationToken cancellationToken = default);

        Task<bool> UpdateFullNameAsync(
            Guid userId,
            string fullName,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken = default);
    }
}
