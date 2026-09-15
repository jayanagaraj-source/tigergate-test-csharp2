using FluentAssertions;
using TigerGateDemo.Core.Entities;
using Xunit;

namespace TigerGateDemo.Tests;

public class OrderTests
{
    private static Product SampleProduct(string sku = "WIDGET-1", decimal price = 25m, int stock = 10) => new()
    {
        Sku = sku,
        Name = $"Product {sku}",
        UnitPrice = price,
        StockOnHand = stock
    };

    [Fact]
    public void AddLine_merges_quantities_for_the_same_product()
    {
        var order = new Order { Reference = "ORD-1" };
        var product = SampleProduct();

        order.AddLine(product, 2);
        order.AddLine(product, 3);

        order.Lines.Should().HaveCount(1);
        order.Lines.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void Subtotal_sums_all_line_totals()
    {
        var order = new Order { Reference = "ORD-2" };
        order.AddLine(SampleProduct("A-1", 10m), 2);
        order.AddLine(SampleProduct("B-2", 5.5m), 4);

        order.Subtotal.Should().Be(42m);
    }

    [Fact]
    public void Submit_rejects_an_empty_order()
    {
        var order = new Order { Reference = "ORD-3" };

        var act = () => order.Submit();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddLine_is_rejected_once_the_order_leaves_draft()
    {
        var order = new Order { Reference = "ORD-4" };
        order.AddLine(SampleProduct(), 1);
        order.Submit();

        var act = () => order.AddLine(SampleProduct("OTHER-1"), 1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void TryReserve_only_succeeds_within_available_stock(int quantity, bool expected)
    {
        var product = SampleProduct(stock: 10);

        product.TryReserve(quantity).Should().Be(expected);
    }
}
