using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quantora.Application.Services;

namespace Quantora.Api.Services;

/// <summary>
/// Monitors saved paper-trading stop losses without requiring a browser session.
/// This worker never creates entries or submits real broker orders.
/// </summary>
public sealed class PaperStopLossBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaperStopLossBackgroundService> _logger;

    public PaperStopLossBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PaperStopLossBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Paper stop-loss background monitor started.");

        // Run once immediately, then continue on a fixed delay.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAllAccountsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paper stop-loss monitoring cycle failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Paper stop-loss background monitor stopped.");
    }

    private async Task MonitorAllAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaperTradingRepository>();
        var paperTrading = scope.ServiceProvider.GetRequiredService<IPaperTradingService>();
        var userIds = await repository.GetUsersWithOpenStopLossPositionsAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var closedOrders = await paperTrading.MonitorStopLossesForUserAsync(userId, cancellationToken);
                foreach (var order in closedOrders)
                {
                    _logger.LogWarning(
                        "Paper stop-loss triggered for user {UserId}, symbol {Symbol}, quantity {Quantity}, simulated exit {Price}.",
                        userId, order.TradingSymbol, order.Quantity, order.ExecutionPrice);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A single account failure must not prevent other users from being monitored.
                _logger.LogError(ex, "Could not monitor paper stop losses for user {UserId}.", userId);
            }
        }
    }
}
