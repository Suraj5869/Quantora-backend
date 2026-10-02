using Quantora.Application.DTOs.PaperTrading;

namespace Quantora.Application.Services;

public interface IPaperTradingService
{
    Task<PaperAccountDto> GetAccountAsync(CancellationToken cancellationToken = default);
    Task<PaperAccountDto> ResetAccountAsync(CancellationToken cancellationToken = default);
    Task<PaperOrderDto> PlaceOrderAsync(PlacePaperOrderRequest request, CancellationToken cancellationToken = default);
}
