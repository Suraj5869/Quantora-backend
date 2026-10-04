using Quantora.Application.DTOs.PaperTrading;

namespace Quantora.Application.Services;

public interface IPaperTradingRepository
{
    Task<IReadOnlyList<Guid>> GetUsersWithOpenStopLossPositionsAsync(CancellationToken cancellationToken = default);
    Task<PaperAccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PaperAccountDto> ResetAccountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PaperOrderDto> PlaceOrderAsync(Guid userId, PlacePaperOrderRequest request, string side, decimal price, CancellationToken cancellationToken = default);
    Task<PaperOrderDto?> ClosePositionAtStopAsync(Guid userId, string instrumentKey, decimal marketPrice, CancellationToken cancellationToken = default);
}