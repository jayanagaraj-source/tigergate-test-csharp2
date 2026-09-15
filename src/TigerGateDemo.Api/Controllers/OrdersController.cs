using Microsoft.AspNetCore.Mvc;
using TigerGateDemo.Api.Dtos;
using TigerGateDemo.Api.Services;

namespace TigerGateDemo.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orders;

    public OrdersController(OrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("by-customer/{customerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForCustomer(Guid customerId, CancellationToken cancellationToken)
        => Ok(await _orders.GetForCustomerAsync(customerId, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Place(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orders.PlaceAsync(request, cancellationToken);

        return result.Match<IActionResult>(
            order => CreatedAtAction(nameof(GetById), new { id = order.Id }, order),
            error => BadRequest(new ProblemDetails { Title = "Order rejected", Detail = error }));
    }
}
