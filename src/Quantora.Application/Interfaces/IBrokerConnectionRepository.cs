using Quantora.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Interfaces
{
    public interface IBrokerConnectionRepository
    {
        Task<BrokerConnection?> GetAsync(
            Guid userId,
            string broker,
            CancellationToken cancellationToken = default);

        Task UpsertAsync(
            BrokerConnection connection,
            CancellationToken cancellationToken = default);

        Task DisconnectAsync(
            Guid userId,
            string broker,
            CancellationToken cancellationToken = default);
    }
}
