using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using TigerGateDemo.Data;

namespace TigerGateDemo.Worker;

/// <summary>
/// Periodically flags products whose stock has drifted below the reorder threshold.
/// </summary>
public sealed class StockReconciliationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private const int ReorderThreshold = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReconciliationWorker> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public StockReconciliationWorker(IServiceScopeFactory scopeFactory, ILogger<StockReconciliationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<DbUpdateException>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (exception, delay, attempt, _) =>
                    _logger.LogWarning(exception, "Reconciliation attempt {Attempt} failed, retrying in {Delay}", attempt, delay));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(ct => ReconcileAsync(ct), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reconciliation pass failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var lowStock = await context.Products
            .AsNoTracking()
            .Where(p => !p.IsDiscontinued && p.StockOnHand <= ReorderThreshold)
            .OrderBy(p => p.StockOnHand)
            .ToListAsync(cancellationToken);

        if (lowStock.Count == 0)
        {
            _logger.LogDebug("No products below the reorder threshold.");
            return;
        }

        foreach (var product in lowStock)
        {
            _logger.LogInformation(
                "Product {Sku} is low on stock ({StockOnHand} remaining)",
                product.Sku,
                product.StockOnHand);
        }
    }
}
