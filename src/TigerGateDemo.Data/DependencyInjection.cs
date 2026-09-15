using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Data.Repositories;

namespace TigerGateDemo.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<ShopDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("tigergate-demo");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShopDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
