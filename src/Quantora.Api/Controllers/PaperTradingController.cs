using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.DTOs.PaperTrading;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/paper-trading")]
public sealed class PaperTradingController : ControllerBase
{
    private readonly IPaperTradingService _service;
    public PaperTradingController(IPaperTradingService service) => _service = service;

    [HttpGet("account")]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken) =>
        Ok(await _service.GetAccountAsync(cancellationToken));

    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlacePaperOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _service.PlaceOrderAsync(request, cancellationToken);
        return Ok(order);
    }

    [HttpPost("monitor-stops")]
    public async Task<IActionResult> MonitorStops(CancellationToken cancellationToken)
    {
        var closedOrders = await _service.MonitorStopLossesAsync(cancellationToken);
        return Ok(new { checkedAt = DateTimeOffset.UtcNow, triggeredCount = closedOrders.Count, closedOrders });
    }

    [HttpPost("simulate-stop")]
    public async Task<IActionResult> SimulateStop([FromBody] StopLossSimulationRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SimulateStopLossAsync(request, cancellationToken));

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken) =>
        Ok(await _service.ResetAccountAsync(cancellationToken));
}
