using Quantora.Application.DTOs.MarketData;

namespace Quantora.Application.Services;

public interface IMarketDataService
{
    Task<MarketCandlesResponseDto> GetHistoricalCandlesAsync(
        string instrumentKey,
        string unit,
        int interval,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<MarketCandlesResponseDto> GetIntradayCandlesAsync(
        string instrumentKey,
        string unit,
        int interval,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketInstrumentDto>> SearchInstrumentsAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<MarketDiscoveryResponseDto> GetMarketDiscoveryAsync(
        CancellationToken cancellationToken = default);
}
