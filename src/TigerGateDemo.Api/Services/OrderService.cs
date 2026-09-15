using TigerGateDemo.Api.Dtos;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Api.Services;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IPricingPolicy _pricing;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        IPricingPolicy pricing,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _products = products;
        _pricing = pricing;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> PlaceAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            return Result<OrderResponse>.Failure("An order must contain at least one line.");
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Reference = BuildReference()
        };

        foreach (var line in request.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<OrderResponse>.Failure($"Product {line.ProductId} does not exist.");
            }

            if (!product.TryReserve(line.Quantity))
            {
                return Result<OrderResponse>.Failure(
                    $"Insufficient stock for '{product.Name}': requested {line.Quantity}, available {product.StockOnHand}.");
            }

            order.AddLine(product, line.Quantity);
        }

        order.Submit();

        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Placed order {Reference} with {LineCount} lines", order.Reference, order.Lines.Count);

        return Result<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<OrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetForCustomerAsync(customerId, cancellationToken);
        return orders.Select(ToResponse).ToList();
    }

    private static string BuildReference()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private OrderResponse ToResponse(Order order)
    {
        var subtotal = order.Subtotal;
        var discount = order.Customer is null ? 0m : _pricing.DiscountFor(order.Customer, subtotal);
        var total = Math.Round((subtotal - discount) * (1 + _pricing.TaxRate), 2);

        return new OrderResponse(
            order.Id,
            order.Reference,
            order.CustomerId,
            order.Status.ToString(),
            order.PlacedAt,
            subtotal,
            discount,
            total,
            order.Lines
                .Select(l => new OrderLineResponse(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
                .ToList());
    }
}
