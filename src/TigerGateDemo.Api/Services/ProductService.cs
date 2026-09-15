using TigerGateDemo.Api.Dtos;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Core.Entities;

namespace TigerGateDemo.Api.Services;

public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository products, IUnitOfWork unitOfWork, ILogger<ProductService> logger)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResponse<ProductResponse>> SearchAsync(
        string? term,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _products.SearchAsync(term, page, pageSize, cancellationToken);

        return new PagedResponse<ProductResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<Result<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _products.GetBySkuAsync(request.Sku, cancellationToken);
        if (existing is not null)
        {
            return Result<ProductResponse>.Failure($"A product with SKU '{request.Sku}' already exists.");
        }

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            StockOnHand = request.StockOnHand
        };

        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created product {Sku} ({ProductId})", product.Sku, product.Id);

        return Result<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<Result<ProductResponse>> RestockAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<ProductResponse>.Failure("Product not found.");
        }

        if (quantity <= 0)
        {
            return Result<ProductResponse>.Failure("Restock quantity must be positive.");
        }

        product.Restock(quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var removed = await _products.RemoveAsync(id, cancellationToken);
        if (removed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return removed;
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.UnitPrice,
        product.StockOnHand,
        product.IsAvailable);
}
