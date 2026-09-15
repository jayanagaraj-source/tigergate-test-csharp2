namespace TigerGateDemo.Core.Entities;

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Paid = 2,
    Shipped = 3,
    Cancelled = 4
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();

    public decimal Subtotal => Lines.Sum(line => line.LineTotal);

    public decimal Total(decimal taxRate) => Math.Round(Subtotal * (1 + taxRate), 2);

    public void AddLine(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot modify an order in status {Status}.");
        }

        var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            return;
        }

        Lines.Add(new OrderLine
        {
            OrderId = Id,
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = quantity
        });
    }

    public void Submit()
    {
        if (Lines.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one line before submission.");
        }

        Status = OrderStatus.Submitted;
    }
}

public class OrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
