namespace TigerGateDemo.Api.Dtos;

public sealed record OrderLineRequest(Guid ProductId, int Quantity);

public sealed record PlaceOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    string Reference,
    Guid CustomerId,
    string Status,
    DateTimeOffset PlacedAt,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
