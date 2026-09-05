using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quantora.Application.Modules.Broker.Commands.CancelSandboxOrder;
using Quantora.Application.Modules.Broker.Commands.ConnectBroker;
using Quantora.Application.Modules.Broker.Commands.ModifySandboxOrder;
using Quantora.Application.Modules.Broker.Commands.PlaceSandboxOrder;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Queries.GetBrokerConnection;
using Quantora.Application.Modules.Broker.Queries.GetOrderDetails;

namespace Quantora.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/brokers")]
    public sealed class BrokerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BrokerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("upstox")]
        [ProducesResponseType(
            typeof(BrokerConnectionResponse),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpstoxConnection(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBrokerConnectionQuery(),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("connect")]
        public async Task<IActionResult> Connect(
    CancellationToken cancellationToken)
        {
            var authorizationUrl =
                await _mediator.Send(
                    new ConnectBrokerCommand(),
                    cancellationToken);

            return Ok(new
            {
                authorizationUrl
            });
        }

        [HttpPost("upstox/sandbox/orders")]
        [ProducesResponseType(
    typeof(UpstoxOrderResponse),
    StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceSandboxOrder(
    [FromBody] UpstoxPlaceOrderRequest request,
    CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new PlaceSandboxOrderCommand(request),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("upstox/orders/{orderId}")]
        [ProducesResponseType(
    typeof(UpstoxOrderDetailsResponse),
    StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderDetails(
    string orderId,
    CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetOrderDetailsQuery(orderId),
                cancellationToken);

            return Ok(result);
        }

        [HttpPut("upstox/sandbox/orders/{orderId}")]
        [ProducesResponseType(
    typeof(UpstoxModifyOrderResponse),
    StatusCodes.Status200OK)]
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

            var result = await _mediator.Send(
                new ModifySandboxOrderCommand(modifiedRequest),
                cancellationToken);

            return Ok(result);
        }

        [HttpDelete("upstox/sandbox/orders/{orderId}")]
        [ProducesResponseType(
    typeof(UpstoxCancelOrderResponse),
    StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelSandboxOrder(
    string orderId,
    CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CancelSandboxOrderCommand(orderId),
                cancellationToken);

            return Ok(result);
        }
    }
}
