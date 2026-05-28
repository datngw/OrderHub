using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Domain.Products;
using OrderHub.Infrastructure.Persistence;

namespace OrderHub.IntegrationTests.Helpers;

public static class DatabaseHelper
{
    public static async Task<Guid> SeedProductAsync(this IntegrationTestFixture fixture,
        int stock = 100, decimal price = 10.00m, string? sku = null, string name = "Test Product")
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();

        var product = new Product
        {
            SKU = sku ?? $"SKU-TEST-{Guid.NewGuid():N}",
            Name = name,
            Description = $"Description for {name}",
            Price = price,
            Stock = stock,
            Category = "Test"
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    public static async Task<Product> GetProductAsync(this IntegrationTestFixture fixture, Guid productId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        return (await db.Products.FindAsync(productId))!;
    }

    public static async Task<int> GetOrderCountAsync(this IntegrationTestFixture fixture)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        return await db.Orders.CountAsync();
    }
}
