using Quantora.Domain.Entities;
namespace Quantora.Application.Interfaces;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, DateTimeOffset loginTime, CancellationToken cancellationToken = default);
}
