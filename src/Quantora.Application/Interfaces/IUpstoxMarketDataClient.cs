using Quantora.Application.DTOs.MarketData;

namespace Quantora.Application.Interfaces;

public interface IUpstoxMarketDataClient
{
    Task<IReadOnlyList<MarketCandleDto>> GetHistoricalCandlesAsync(
        string accessToken,
        string instrumentKey,
        string unit,
        int interval,
        DateOnly toDate,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketCandleDto>> GetIntradayCandlesAsync(
        string accessToken,
        string instrumentKey,
        string unit,
        int interval,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketInstrumentDto>> SearchInstrumentsAsync(
        string accessToken,
        string query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketMoverDto>> GetQuotesAsync(
        string accessToken,
        IReadOnlyList<MarketInstrumentDto> instruments,
        CancellationToken cancellationToken = default);
}
