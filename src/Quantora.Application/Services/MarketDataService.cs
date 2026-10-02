using Quantora.Application.Common.Interfaces;
using Quantora.Application.DTOs.MarketData;
using Quantora.Application.Interfaces;

namespace Quantora.Application.Services;

public sealed class MarketDataService : IMarketDataService
{
    private readonly IUpstoxMarketDataClient _marketDataClient;
    private readonly IBrokerConnectionRepository _connectionRepository;
    private readonly ISecretProtector _secretProtector;
    private readonly ICurrentUserService _currentUserService;

    public MarketDataService(
        IUpstoxMarketDataClient marketDataClient,
        IBrokerConnectionRepository connectionRepository,
        ISecretProtector secretProtector,
        ICurrentUserService currentUserService)
    {
        _marketDataClient = marketDataClient;
        _connectionRepository = connectionRepository;
        _secretProtector = secretProtector;
        _currentUserService = currentUserService;
    }

    public async Task<MarketCandlesResponseDto> GetHistoricalCandlesAsync(
        string instrumentKey,
        string unit,
        int interval,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        ValidateInstrumentKey(instrumentKey);
        ValidateUnitAndInterval(unit, interval);

        if (fromDate > toDate)
            throw new ArgumentException("fromDate must be on or before toDate.");

        if (toDate > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            throw new ArgumentException("toDate cannot be in the future.");

        var maximumDays = GetMaximumHistoricalDays(unit, interval);
        if (maximumDays is not null &&
            toDate.DayNumber - fromDate.DayNumber > maximumDays.Value)
        {
            throw new ArgumentException(
                $"The requested date range is too large for {unit}/{interval}. " +
                $"Use a range of {maximumDays.Value} days or less.");
        }

        var accessToken = await GetConnectedAccessTokenAsync(cancellationToken);
        var candles = await _marketDataClient.GetHistoricalCandlesAsync(
            accessToken,
            instrumentKey,
            unit,
            interval,
            toDate,
            fromDate,
            cancellationToken);

        return new MarketCandlesResponseDto
        {
            InstrumentKey = instrumentKey,
            Unit = unit,
            Interval = interval,
            FromDate = fromDate,
            ToDate = toDate,
            FetchedAt = DateTimeOffset.UtcNow,
            Candles = candles
        };
    }

    public async Task<MarketCandlesResponseDto> GetIntradayCandlesAsync(
        string instrumentKey,
        string unit,
        int interval,
        CancellationToken cancellationToken = default)
    {
        ValidateInstrumentKey(instrumentKey);
        ValidateUnitAndInterval(unit, interval);

        if (unit is not ("minutes" or "hours" or "days"))
            throw new ArgumentException(
                "Intraday candles support minutes, hours, or days.");

        var accessToken = await GetConnectedAccessTokenAsync(cancellationToken);
        var candles = await _marketDataClient.GetIntradayCandlesAsync(
            accessToken,
            instrumentKey,
            unit,
            interval,
            cancellationToken);

        return new MarketCandlesResponseDto
        {
            InstrumentKey = instrumentKey,
            Unit = unit,
            Interval = interval,
            FetchedAt = DateTimeOffset.UtcNow,
            Candles = candles
        };
    }

    private async Task<string> GetConnectedAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetAsync(
            _currentUserService.UserId,
            "Upstox",
            cancellationToken);

        if (connection is null || !connection.IsActive ||
            string.IsNullOrWhiteSpace(connection.AccessTokenEncrypted))
        {
            throw new UnauthorizedAccessException(
                "Connect your Upstox account before requesting market data.");
        }

        return _secretProtector.Unprotect(connection.AccessTokenEncrypted);
    }

    private static void ValidateInstrumentKey(string instrumentKey)
    {
        if (string.IsNullOrWhiteSpace(instrumentKey) ||
            instrumentKey.Length > 100 ||
            !instrumentKey.Contains('|') ||
            instrumentKey.Any(char.IsControl))
        {
            throw new ArgumentException(
                "A valid Upstox instrumentKey is required.");
        }
    }

    private static void ValidateUnitAndInterval(string unit, int interval)
    {
        if (interval < 1)
            throw new ArgumentException("interval must be greater than zero.");

        var valid = unit switch
        {
            "minutes" => interval <= 300,
            "hours" => interval <= 5,
            "days" or "weeks" or "months" => interval == 1,
            _ => false
        };

        if (!valid)
            throw new ArgumentException(
                "Supported intervals are minutes 1-300, hours 1-5, and days/weeks/months 1.");
    }

    private static int? GetMaximumHistoricalDays(string unit, int interval)
    {
        return unit switch
        {
            "minutes" when interval <= 15 => 31,
            "minutes" => 92,
            "hours" => 92,
            "days" => 3653,
            _ => null
        };
    }
}
