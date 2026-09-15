using Microsoft.AspNetCore.Mvc;
using TigerGateDemo.Api.Dtos;
using TigerGateDemo.Api.Services;

namespace TigerGateDemo.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _products;

    public ProductsController(ProductService products)
    {
        _products = products;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _products.SearchAsync(term, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _products.CreateAsync(request, cancellationToken);

        return result.Match<IActionResult>(
            product => CreatedAtAction(nameof(Search), new { term = product.Sku }, product),
            error => Conflict(new ProblemDetails { Title = "Product not created", Detail = error }));
    }

    [HttpPost("{id:guid}/restock")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restock(
        Guid id,
        [FromBody] RestockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _products.RestockAsync(id, request.Quantity, cancellationToken);

        return result.Match<IActionResult>(
            Ok,
            error => BadRequest(new ProblemDetails { Title = "Restock failed", Detail = error }));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var removed = await _products.DeleteAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
