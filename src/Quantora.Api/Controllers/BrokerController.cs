using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.DTOs.Broker;
using Quantora.Application.Services;

namespace Quantora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/brokers")]
public sealed class BrokerController : ControllerBase
{
    private readonly IBrokerService _brokerService;

    public BrokerController(IBrokerService brokerService)
    {
        _brokerService = brokerService;
    }

    [HttpGet("upstox")]
    public async Task<IActionResult> GetUpstoxConnection(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _brokerService.GetConnectionAsync(
                cancellationToken));
    }

    [HttpGet("upstox/connect")]
    public async Task<IActionResult> ConnectUpstox(
        CancellationToken cancellationToken)
    {
        var authorizationUrl =
            await _brokerService.GetAuthorizationUrlAsync(
                cancellationToken);

        return Ok(new { authorizationUrl });
    }

    [AllowAnonymous]
    [HttpGet("upstox/callback")]
    public async Task<IActionResult> UpstoxCallback(
    [FromQuery] string code,
    [FromQuery] string state,
    CancellationToken cancellationToken)
    {
        var result =
            await _brokerService.HandleOAuthCallbackAsync(
                code,
                state,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("upstox/sandbox/orders")]
    public async Task<IActionResult> PlaceSandboxOrder(
        [FromBody] UpstoxPlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(
            await _brokerService.PlaceSandboxOrderAsync(
                request,
                cancellationToken));
    }

    [HttpGet("upstox/orders/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(
        string orderId,
        CancellationToken cancellationToken)
    {
        return Ok(
            await _brokerService.GetOrderDetailsAsync(
                orderId,
                cancellationToken));
    }

    [HttpPut("upstox/sandbox/orders/{orderId}")]
    public async Task<IActionResult> ModifySandboxOrder(
        string orderId,
        [FromBody] UpstoxModifyOrderRequest request,
        CancellationToken cancellationToken)
    {
        var modifiedRequest = new UpstoxModifyOrderRequest
        {
            OrderId = orderId,
            Quantity = request.Quantity,
            OrderType = request.OrderType,
            Price = request.Price,
            TriggerPrice = request.TriggerPrice,
            Validity = request.Validity,
            DisclosedQuantity = request.DisclosedQuantity
        };

        return Ok(
            await _brokerService.ModifySandboxOrderAsync(
                modifiedRequest,
                cancellationToken));
    }

    [HttpDelete("upstox/sandbox/orders/{orderId}")]
    public async Task<IActionResult> CancelSandboxOrder(
        string orderId,
        CancellationToken cancellationToken)
    {
        return Ok(
            await _brokerService.CancelSandboxOrderAsync(
                orderId,
                cancellationToken));
    }
}
