using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using OrderHub.Application.Common.Pagination;
using OrderHub.Infrastructure.Persistence;
using OrderHub.IntegrationTests.Shared;

namespace OrderHub.IntegrationTests.Features.Products;

[Collection(nameof(IntegrationTestCollection))]
public class ProductQueryPerformanceTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    private const int ProductCount = 100_000;
    private const int MaxResponseTimeMs = 200;

    private static readonly string[] Categories =
        ["Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food", "Automotive", "Health", "Music"];

    private static bool _seeded;
    private static HttpClient? _client;

    public async Task InitializeAsync()
    {
        if (_seeded)
        {
            // Reuse existing client for subsequent test instances
            return;
        }

        await fixture.ResetDatabaseAsync();
        await BulkSeedProductsAsync();
        await AnalyzeTableAsync();

        // Create one authenticated client, reuse across all tests
        _client = await fixture.CreateAuthenticatedCustomerAsync();

        // Warm-up request to prime EF Core connection pool and JIT
        await _client.GetAsync("/api/v1/products/?page=1&pageSize=1");

        _seeded = true;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DefaultQuery_ReturnsUnder200ms()
    {
        var sw = Stopwatch.StartNew();
        var response = await _client!.GetAsync("/api/v1/products/");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        result!.Items.Should().HaveCount(20);
        result.TotalCount.Should().Be(ProductCount);

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxResponseTimeMs,
            "default query (page 1, no filters) should complete under {0}ms, took {1}ms",
            MaxResponseTimeMs, sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Default query: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CategoryFilter_ReturnsUnder200ms()
    {
        var sw = Stopwatch.StartNew();
        var response = await _client!.GetAsync("/api/v1/products/?category=Electronics");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        result!.Items.Should().HaveCount(20);

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxResponseTimeMs,
            "category filter query should complete under {0}ms, took {1}ms",
            MaxResponseTimeMs, sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Category filter: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task PriceRange_ReturnsUnder200ms()
    {
        var sw = Stopwatch.StartNew();
        var response = await _client!.GetAsync("/api/v1/products/?minPrice=100&maxPrice=500");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxResponseTimeMs,
            "price range query should complete under {0}ms, took {1}ms",
            MaxResponseTimeMs, sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Price range: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task SearchQuery_ReturnsUnder200ms()
    {
        var sw = Stopwatch.StartNew();
        var response = await _client!.GetAsync("/api/v1/products/?search=Widget");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxResponseTimeMs,
            "search query should complete under {0}ms, took {1}ms",
            MaxResponseTimeMs, sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Search query: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task DeepPagination_ReturnsUnder200ms()
    {
        var sw = Stopwatch.StartNew();
        var response = await _client!.GetAsync("/api/v1/products/?page=500&pageSize=20");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        result!.Items.Should().HaveCount(20);

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxResponseTimeMs,
            "deep pagination (page 500) should complete under {0}ms, took {1}ms",
            MaxResponseTimeMs, sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Deep pagination: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CacheHit_ReturnsUnder20ms()
    {
        // First request — cache miss (populates IMemoryCache)
        await _client!.GetAsync("/api/v1/products/?page=1&pageSize=20");

        // Second request — cache hit
        var sw = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/products/?page=1&pageSize=20");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        sw.ElapsedMilliseconds.Should().BeLessThan(20,
            "cache hit should complete under 20ms (includes HTTP overhead), took {0}ms",
            sw.ElapsedMilliseconds);

        Console.WriteLine($"\n[PERF] Cache hit: {sw.ElapsedMilliseconds}ms");
    }

    private async Task BulkSeedProductsAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        var connectionString = db.Database.GetConnectionString();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var writer = conn.BeginBinaryImport(
            @"COPY ""Products"" (""Id"", ""SKU"", ""Name"", ""Description"", ""Price"", ""Stock"", ""Category"", ""IsActive"", ""CreatedAt"") FROM STDIN (FORMAT BINARY)");

        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var random = new Random(42);

        for (int i = 0; i < ProductCount; i++)
        {
            writer.StartRow();
            writer.Write(Guid.NewGuid(), NpgsqlDbType.Uuid);
            writer.Write($"SKU-{i:D6}", NpgsqlDbType.Varchar);
            writer.Write(GenerateName(i, random), NpgsqlDbType.Varchar);
            writer.Write($"High-quality product description for item {i}. Premium materials and craftsmanship.", NpgsqlDbType.Varchar);
            writer.Write(Math.Round((decimal)(random.NextDouble() * 990 + 10), 2), NpgsqlDbType.Numeric);
            writer.Write(random.Next(0, 501), NpgsqlDbType.Integer);
            writer.Write(Categories[i % Categories.Length], NpgsqlDbType.Varchar);
            writer.Write(true, NpgsqlDbType.Boolean);
            writer.Write(baseDate.AddSeconds(i), NpgsqlDbType.Timestamp);
        }

        await writer.CompleteAsync();
    }

    private static string GenerateName(int index, Random random)
    {
        var adjectives = new[] { "Premium", "Deluxe", "Classic", "Ultra", "Pro", "Smart", "Eco", "Elite", "Mega", "Super" };
        var nouns = new[] { "Widget", "Gadget", "Device", "Tool", "Kit", "Set", "Pack", "Bundle", "Station", "Hub" };
        var adj = adjectives[index % adjectives.Length];
        var noun = nouns[random.Next(nouns.Length)];
        return $"{adj} {noun} #{index}";
    }

    private async Task AnalyzeTableAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"ANALYZE ""Products""");
    }
}
