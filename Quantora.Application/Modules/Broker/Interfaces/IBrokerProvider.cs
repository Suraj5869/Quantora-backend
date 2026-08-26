using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Broker.Interfaces
{
    public interface IBrokerProvider
    {
        string BrokerName { get; }

        bool IsSandbox { get; }

        Task<bool> IsConfiguredAsync(
            CancellationToken cancellationToken = default);
    }
}
