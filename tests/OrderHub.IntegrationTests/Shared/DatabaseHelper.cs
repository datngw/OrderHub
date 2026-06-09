using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Products;
using OrderHub.Domain.Users;
using OrderHub.Infrastructure.Persistence;

namespace OrderHub.IntegrationTests.Shared;

public static class DatabaseHelper
{
    private static int _userCounter;

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

    /// <summary>
    /// Seeds a customer user directly into the DB (bypasses the register HTTP endpoint and its
    /// 3-req/min rate limit). Then logs in via the login endpoint to obtain a valid JWT token.
    /// Use this in tests that need more than 3 authenticated clients.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientDirectAsync(
        this IntegrationTestFixture fixture)
    {
        const string password = "Test@12345";
        var counter = Interlocked.Increment(ref _userCounter);
        var email   = $"direct-{counter}-{Guid.NewGuid():N}@test.com";

        // Insert user directly into DB — no HTTP round-trip, no rate limit
        using (var scope = fixture.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var hasher = new PasswordHasher<User>();
            var user   = new User { Email = email, FullName = "Test Customer" };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Login via HTTP to get a valid JWT (login rate limit is 5/min — generous)
        var client   = fixture.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = email, Password = password });
        loginRes.EnsureSuccessStatusCode();

        var auth = (await loginRes.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
