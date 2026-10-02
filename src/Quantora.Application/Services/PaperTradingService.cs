using Quantora.Application.Common.Interfaces;
using Quantora.Application.DTOs.PaperTrading;
using Quantora.Application.Services;

namespace Quantora.Application.Services;

public sealed class PaperTradingService : IPaperTradingService
{
    private readonly IPaperTradingRepository _repository;
    private readonly IMarketDataService _marketData;
    private readonly ICurrentUserService _currentUser;
    public PaperTradingService(IPaperTradingRepository repository, IMarketDataService marketData, ICurrentUserService currentUser)
    { _repository = repository; _marketData = marketData; _currentUser = currentUser; }

    public Task<PaperAccountDto> GetAccountAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAccountAsync(RequireUser(), cancellationToken);

    public Task<PaperAccountDto> ResetAccountAsync(CancellationToken cancellationToken = default) =>
        _repository.ResetAccountAsync(RequireUser(), cancellationToken);

    public async Task<PaperOrderDto> PlaceOrderAsync(PlacePaperOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        if (string.IsNullOrWhiteSpace(request.InstrumentKey) || request.InstrumentKey.Length > 120 ||
            !request.InstrumentKey.StartsWith("NSE_EQ|", StringComparison.Ordinal))
            throw new ArgumentException("Select a valid NSE equity instrument.");
        if (string.IsNullOrWhiteSpace(request.TradingSymbol) || request.TradingSymbol.Length > 40)
            throw new ArgumentException("A valid trading symbol is required.");
        var side = request.Side.Trim().ToUpperInvariant();
        if (side is not ("BUY" or "SELL")) throw new ArgumentException("Side must be BUY or SELL.");
        if (request.Quantity <= 0 || request.Quantity > 1000000 || decimal.Round(request.Quantity, 4) != request.Quantity)
            throw new ArgumentException("Quantity must be greater than zero, at most 1,000,000, and use no more than 4 decimal places.");
        var candles = await _marketData.GetIntradayCandlesAsync(request.InstrumentKey, "minutes", 1, cancellationToken);
        var latest = candles.Candles.OrderByDescending(c => c.Timestamp).FirstOrDefault();
        if (latest is null || latest.Close <= 0) throw new InvalidOperationException("A current market price is unavailable; no paper order was placed.");
        return await _repository.PlaceOrderAsync(userId, request, side, latest.Close, cancellationToken);
    }

    private Guid RequireUser() => _currentUser.IsAuthenticated && _currentUser.UserId != Guid.Empty
        ? _currentUser.UserId : throw new UnauthorizedAccessException("An authenticated user is required.");
}
