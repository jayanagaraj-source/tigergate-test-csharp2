using Microsoft.EntityFrameworkCore;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Data.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ShopDbContext _context;

    public ProductRepository(ShopDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        => _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

    public async Task<PagedList<Product>> SearchAsync(
        string? term,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern) || EF.Functions.Like(p.Sku, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Product>(items, page, pageSize, total);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _context.Products.Remove(product);
        return true;
    }
}
