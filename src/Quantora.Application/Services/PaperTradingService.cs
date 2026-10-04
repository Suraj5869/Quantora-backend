using Quantora.Application.Common.Interfaces;
using Quantora.Application.DTOs.PaperTrading;
using Quantora.Application.Services;

namespace Quantora.Application.Services;

public sealed class PaperTradingService : IPaperTradingService
{
    private const decimal MaximumRiskPercent = 1m;
    private readonly IPaperTradingRepository _repository;
    private readonly IMarketDataService _marketData;
    private readonly ICurrentUserService _currentUser;
    public PaperTradingService(IPaperTradingRepository repository, IMarketDataService marketData, ICurrentUserService currentUser)
    { _repository = repository; _marketData = marketData; _currentUser = currentUser; }

    public async Task<PaperAccountDto> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountAsync(RequireUser(), cancellationToken);
        var valuedPositions = new List<PaperPositionDto>(account.Positions.Count);

        foreach (var position in account.Positions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lastPrice = position.AveragePrice;
            try
            {
                var candles = await _marketData.GetIntradayCandlesAsync(
                    position.InstrumentKey, "minutes", 1, cancellationToken);
                var latest = candles.Candles.OrderByDescending(c => c.Timestamp).FirstOrDefault();
                if (latest is not null && latest.Close > 0)
                    lastPrice = latest.Close;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Keep the last known cost basis if market data is temporarily unavailable.
            }

            valuedPositions.Add(new PaperPositionDto
            {
                InstrumentKey = position.InstrumentKey,
                TradingSymbol = position.TradingSymbol,
                Quantity = position.Quantity,
                AveragePrice = position.AveragePrice,
                LastPrice = lastPrice,
                StopLossPrice = position.StopLossPrice,
                MarketValue = decimal.Round(position.Quantity * lastPrice, 2),
                UnrealizedPnl = decimal.Round((lastPrice - position.AveragePrice) * position.Quantity, 2)
            });
        }

        var investedValue = valuedPositions.Sum(p => p.MarketValue);
        var portfolioValue = account.AvailableCash + investedValue;
        return new PaperAccountDto
        {
            Id = account.Id,
            InitialCash = account.InitialCash,
            AvailableCash = account.AvailableCash,
            InvestedValue = investedValue,
            PortfolioValue = portfolioValue,
            TotalPnl = decimal.Round(portfolioValue - account.InitialCash, 2),
            Positions = valuedPositions,
            RecentOrders = account.RecentOrders
        };
    }

    public async Task<PaperAccountDto> ResetAccountAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ResetAccountAsync(RequireUser(), cancellationToken);
        return await GetAccountAsync(cancellationToken);
    }

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
        if (side == "BUY")
        {
            if (request.StopLossPrice is null || request.StopLossPrice <= 0 || request.StopLossPrice >= latest.Close)
                throw new ArgumentException("For BUY orders, set a stop-loss price greater than zero and below the current reference price.");
            var account = await _repository.GetAccountAsync(userId, cancellationToken);
            var plannedRisk = decimal.Round((latest.Close - request.StopLossPrice.Value) * request.Quantity, 2, MidpointRounding.AwayFromZero);
            var riskLimit = decimal.Round(account.PortfolioValue * MaximumRiskPercent / 100m, 2, MidpointRounding.AwayFromZero);
            if (plannedRisk > riskLimit)
                throw new ArgumentException($"Order rejected by risk controls: planned loss at stop ({plannedRisk:C}) exceeds the 1% per-order limit ({riskLimit:C}). Reduce quantity or use the risk sizing preview.");
        }
        return await _repository.PlaceOrderAsync(userId, request, side, latest.Close, cancellationToken);
    }

    public async Task<IReadOnlyList<PaperOrderDto>> MonitorStopLossesAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        var account = await _repository.GetAccountAsync(userId, cancellationToken);
        var triggered = new List<PaperOrderDto>();
        foreach (var position in account.Positions.Where(p => p.StopLossPrice is not null && p.StopLossPrice > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var candles = await _marketData.GetIntradayCandlesAsync(position.InstrumentKey, "minutes", 1, cancellationToken);
                var latest = candles.Candles.OrderByDescending(c => c.Timestamp).FirstOrDefault();
                if (latest is null || latest.Close <= 0 || latest.Close > position.StopLossPrice!.Value)
                    continue;
                var closed = await _repository.ClosePositionAtStopAsync(userId, position.InstrumentKey, latest.Close, cancellationToken);
                if (closed is not null) triggered.Add(closed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                // A temporary quote failure must not close a position using stale or fallback prices.
            }
        }
        return triggered;
    }

    private Guid RequireUser() => _currentUser.IsAuthenticated && _currentUser.UserId != Guid.Empty
        ? _currentUser.UserId : throw new UnauthorizedAccessException("An authenticated user is required.");
}
