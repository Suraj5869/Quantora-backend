namespace Quantora.Application.DTOs.MarketData;

public sealed class MarketDiscoveryResponseDto
{
    public DateTimeOffset FetchedAt { get; init; }
    public IReadOnlyList<MarketMoverDto> FeaturedStocks { get; init; } = Array.Empty<MarketMoverDto>();
    public IReadOnlyList<MarketMoverDto> TopGainers { get; init; } = Array.Empty<MarketMoverDto>();
    public IReadOnlyList<MarketMoverDto> TopLosers { get; init; } = Array.Empty<MarketMoverDto>();
}
