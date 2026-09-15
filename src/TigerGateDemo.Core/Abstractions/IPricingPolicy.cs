using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Core.Abstractions;

public interface IPricingPolicy
{
    decimal TaxRate { get; }
    decimal DiscountFor(Customer customer, decimal subtotal);
}
