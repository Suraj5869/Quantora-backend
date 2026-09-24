using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Interfaces
{
    public interface IBrokerOAuthStateRepository
    {
        Task CreateAsync(
            Guid userId,
            string broker,
            string stateHash,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default);

        Task<Guid?> ConsumeAsync(
            string stateHash,
            CancellationToken cancellationToken = default);
    }
}
