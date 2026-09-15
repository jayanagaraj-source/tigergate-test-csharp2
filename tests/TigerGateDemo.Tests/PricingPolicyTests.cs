using FluentAssertions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Core.Entities;
using Xunit;

namespace TigerGateDemo.Tests;

public class PricingPolicyTests
{
    private readonly TieredPricingPolicy _policy = new();

    [Theory]
    [InlineData(500, 0)]
    [InlineData(1000, 40)]
    [InlineData(5000, 400)]
    [InlineData(10000, 1200)]
    public void DiscountFor_applies_the_volume_tier(decimal subtotal, decimal expected)
    {
        var customer = new Customer { FullName = "Retail Buyer", Email = "buyer@example.com" };

        _policy.DiscountFor(customer, subtotal).Should().Be(expected);
    }

    [Fact]
    public void Business_accounts_receive_an_additional_two_percent()
    {
        var customer = new Customer
        {
            FullName = "Ops Lead",
            Email = "ops@contoso.com",
            CompanyName = "Contoso Ltd"
        };

        _policy.DiscountFor(customer, 1000m).Should().Be(60m);
    }
}
