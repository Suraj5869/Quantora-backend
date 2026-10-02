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

    private static readonly IReadOnlyList<MarketInstrumentDto> FeaturedUniverse =
    [
        new() { Name = "Reliance Industries", TradingSymbol = "RELIANCE", InstrumentKey = "NSE_EQ|INE002A01018" },
        new() { Name = "Tata Consultancy Services", TradingSymbol = "TCS", InstrumentKey = "NSE_EQ|INE467B01029" },
        new() { Name = "HDFC Bank", TradingSymbol = "HDFCBANK", InstrumentKey = "NSE_EQ|INE040A01034" },
        new() { Name = "ICICI Bank", TradingSymbol = "ICICIBANK", InstrumentKey = "NSE_EQ|INE090A01021" },
        new() { Name = "Infosys", TradingSymbol = "INFY", InstrumentKey = "NSE_EQ|INE009A01021" },
        new() { Name = "State Bank of India", TradingSymbol = "SBIN", InstrumentKey = "NSE_EQ|INE062A01020" },
        new() { Name = "ITC", TradingSymbol = "ITC", InstrumentKey = "NSE_EQ|INE154A01025" },
        new() { Name = "Bharti Airtel", TradingSymbol = "BHARTIARTL", InstrumentKey = "NSE_EQ|INE397D01024" },
        new() { Name = "Larsen & Toubro", TradingSymbol = "LT", InstrumentKey = "NSE_EQ|INE018A01030" },
        new() { Name = "Axis Bank", TradingSymbol = "AXISBANK", InstrumentKey = "NSE_EQ|INE238A01034" },
        new() { Name = "Kotak Mahindra Bank", TradingSymbol = "KOTAKBANK", InstrumentKey = "NSE_EQ|INE237A01028" },
        new() { Name = "Bajaj Finance", TradingSymbol = "BAJFINANCE", InstrumentKey = "NSE_EQ|INE296A01024" },
        new() { Name = "Wipro", TradingSymbol = "WIPRO", InstrumentKey = "NSE_EQ|INE075A01022" },
        new() { Name = "Hindustan Unilever", TradingSymbol = "HINDUNILVR", InstrumentKey = "NSE_EQ|INE030A01027" },
        new() { Name = "Maruti Suzuki India", TradingSymbol = "MARUTI", InstrumentKey = "NSE_EQ|INE585B01010" },
        new() { Name = "Tata Steel", TradingSymbol = "TATASTEEL", InstrumentKey = "NSE_EQ|INE081A01020" },
        new() { Name = "NTPC", TradingSymbol = "NTPC", InstrumentKey = "NSE_EQ|INE733E01010" },
        new() { Name = "Power Grid Corporation", TradingSymbol = "POWERGRID", InstrumentKey = "NSE_EQ|INE752E01010" },
        new() { Name = "Sun Pharmaceutical", TradingSymbol = "SUNPHARMA", InstrumentKey = "NSE_EQ|INE044A01036" },
        new() { Name = "Adani Enterprises", TradingSymbol = "ADANIENT", InstrumentKey = "NSE_EQ|INE423A01024" }
    ];

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
        string instrumentKey, string unit, int interval, DateOnly fromDate,
        DateOnly toDate, CancellationToken cancellationToken = default)
    {
        ValidateInstrumentKey(instrumentKey);
        ValidateUnitAndInterval(unit, interval);
        if (fromDate > toDate)
            throw new ArgumentException("fromDate must be on or before toDate.");
        if (toDate > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            throw new ArgumentException("toDate cannot be in the future.");

        var maximumDays = GetMaximumHistoricalDays(unit, interval);
        if (maximumDays is not null && toDate.DayNumber - fromDate.DayNumber > maximumDays.Value)
            throw new ArgumentException("The requested date range is too large for " + unit + "/" + interval + ".");

        var accessToken = await GetConnectedAccessTokenAsync(cancellationToken);
        var candles = await _marketDataClient.GetHistoricalCandlesAsync(
            accessToken, instrumentKey, unit, interval, toDate, fromDate, cancellationToken);

        return new MarketCandlesResponseDto
        {
            InstrumentKey = instrumentKey, Unit = unit, Interval = interval,
            FromDate = fromDate, ToDate = toDate, FetchedAt = DateTimeOffset.UtcNow, Candles = candles
        };
    }

    public async Task<MarketCandlesResponseDto> GetIntradayCandlesAsync(
        string instrumentKey, string unit, int interval, CancellationToken cancellationToken = default)
    {
        ValidateInstrumentKey(instrumentKey);
        ValidateUnitAndInterval(unit, interval);
        var accessToken = await GetConnectedAccessTokenAsync(cancellationToken);
        var candles = await _marketDataClient.GetIntradayCandlesAsync(
            accessToken, instrumentKey, unit, interval, cancellationToken);

        return new MarketCandlesResponseDto
        {
            InstrumentKey = instrumentKey, Unit = unit, Interval = interval,
            FetchedAt = DateTimeOffset.UtcNow, Candles = candles
        };
    }

    public async Task<IReadOnlyList<MarketInstrumentDto>> SearchInstrumentsAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Array.Empty<MarketInstrumentDto>();
        if (query.Trim().Length > 50)
            throw new ArgumentException("Search text must be 50 characters or fewer.");

        var token = await GetConnectedAccessTokenAsync(cancellationToken);
        return await _marketDataClient.SearchInstrumentsAsync(token, query.Trim(), cancellationToken);
    }

    public async Task<MarketDiscoveryResponseDto> GetMarketDiscoveryAsync(
        CancellationToken cancellationToken = default)
    {
        var token = await GetConnectedAccessTokenAsync(cancellationToken);
        var quotes = await _marketDataClient.GetQuotesAsync(token, FeaturedUniverse, cancellationToken);
        var featured = quotes.OrderBy(x => x.TradingSymbol).ToArray();

        return new MarketDiscoveryResponseDto
        {
            FetchedAt = DateTimeOffset.UtcNow,
            FeaturedStocks = featured,
            TopGainers = quotes.Where(x => x.PreviousClose > 0 && x.ChangePercent > 0)
                .OrderByDescending(x => x.ChangePercent).Take(5).ToArray(),
            TopLosers = quotes.Where(x => x.PreviousClose > 0 && x.ChangePercent < 0)
                .OrderBy(x => x.ChangePercent).Take(5).ToArray()
        };
    }

    private async Task<string> GetConnectedAccessTokenAsync(CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetAsync(
            _currentUserService.UserId, "Upstox", cancellationToken);
        if (connection is null || !connection.IsActive ||
            string.IsNullOrWhiteSpace(connection.AccessTokenEncrypted))
            throw new UnauthorizedAccessException("Connect your Upstox account to view live market data.");

        return _secretProtector.Unprotect(connection.AccessTokenEncrypted);
    }

    private static void ValidateInstrumentKey(string instrumentKey)
    {
        if (string.IsNullOrWhiteSpace(instrumentKey) || instrumentKey.Length > 100 ||
            !instrumentKey.Contains('|') || instrumentKey.Any(char.IsControl))
            throw new ArgumentException("A valid Upstox instrument key is required.");
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
            throw new ArgumentException("Supported intervals are minutes 1-300, hours 1-5, and days/weeks/months 1.");
    }

    private static int? GetMaximumHistoricalDays(string unit, int interval) => unit switch
    {
        "minutes" when interval <= 15 => 31,
        "minutes" => 92,
        "hours" => 92,
        "days" => 3653,
        _ => null
    };
}
