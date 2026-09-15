using Microsoft.EntityFrameworkCore;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Data.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ShopDbContext _context;

    public OrderRepository(ShopDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
        => await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await _context.Orders.AddAsync(order, cancellationToken);
    }
}
