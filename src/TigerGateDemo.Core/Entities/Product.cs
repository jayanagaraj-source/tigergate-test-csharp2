namespace TigerGateDemo.Core.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockOnHand { get; set; }
    public bool IsDiscontinued { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsAvailable => !IsDiscontinued && StockOnHand > 0;

    public void Restock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Restock quantity must be positive.");
        }

        StockOnHand += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0 || quantity > StockOnHand)
        {
            return false;
        }

        StockOnHand -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
