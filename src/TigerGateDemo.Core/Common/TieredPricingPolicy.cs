using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Core.Common;

/// <summary>
/// Volume discounts, with an extra tier for business accounts.
/// </summary>
public sealed class TieredPricingPolicy : IPricingPolicy
{
    public decimal TaxRate => 0.08m;

    public decimal DiscountFor(Customer customer, decimal subtotal)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var rate = subtotal switch
        {
            >= 10_000m => 0.12m,
            >= 5_000m => 0.08m,
            >= 1_000m => 0.04m,
            _ => 0m
        };

        if (customer.IsBusinessAccount)
        {
            rate += 0.02m;
        }

        return Math.Round(subtotal * rate, 2);
    }
}
