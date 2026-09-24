using Quantora.Domain.Entities;
namespace Quantora.Application.Interfaces;
public interface IProfileRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateFullNameAsync(Guid userId, string fullName, DateTimeOffset updatedAt, CancellationToken cancellationToken = default);
}
