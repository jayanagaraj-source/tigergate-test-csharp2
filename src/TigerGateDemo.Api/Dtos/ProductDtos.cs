using FluentValidation;

namespace TigerGateDemo.Api.Dtos;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockOnHand,
    bool IsAvailable);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockOnHand);

public sealed record RestockRequest(int Quantity);

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Z0-9-]+$")
            .WithMessage("SKU may only contain upper-case letters, digits and hyphens.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.UnitPrice).GreaterThan(0m);
        RuleFor(x => x.StockOnHand).GreaterThanOrEqualTo(0);
    }
}
