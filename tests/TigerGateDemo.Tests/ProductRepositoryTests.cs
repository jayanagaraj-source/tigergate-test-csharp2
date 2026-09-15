using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TigerGateDemo.Core.Entities;
using TigerGateDemo.Data;
using TigerGateDemo.Data.Repositories;
using Xunit;

namespace TigerGateDemo.Tests;

public class ProductRepositoryTests : IDisposable
{
    private readonly ShopDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase($"products-{Guid.NewGuid()}")
            .Options;

        _context = new ShopDbContext(options);
        _repository = new ProductRepository(_context);
    }

    [Fact]
    public async Task SearchAsync_pages_results()
    {
        for (var i = 1; i <= 30; i++)
        {
            await _repository.AddAsync(new Product
            {
                Sku = $"SKU-{i:D3}",
                Name = $"Product {i:D3}",
                UnitPrice = i
            });
        }

        await _context.SaveChangesAsync();

        var page = await _repository.SearchAsync(term: null, page: 2, pageSize: 10);

        page.Items.Should().HaveCount(10);
        page.TotalCount.Should().Be(30);
        page.TotalPages.Should().Be(3);
        page.HasNext.Should().BeTrue();
        page.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_returns_false_for_an_unknown_id()
    {
        (await _repository.RemoveAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task GetBySkuAsync_finds_a_stored_product()
    {
        await _repository.AddAsync(new Product { Sku = "FIND-ME", Name = "Findable", UnitPrice = 9.99m });
        await _context.SaveChangesAsync();

        var found = await _repository.GetBySkuAsync("FIND-ME");

        found.Should().NotBeNull();
        found!.Name.Should().Be("Findable");
    }

    public void Dispose() => _context.Dispose();
}
