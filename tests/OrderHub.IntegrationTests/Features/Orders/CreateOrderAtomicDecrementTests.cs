using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Domain.Orders;
using OrderHub.Infrastructure.Persistence;
using OrderHub.IntegrationTests.Shared;

namespace OrderHub.IntegrationTests.Features.Orders;

/// <summary>
/// Integration tests that verify the Atomic Decrement implementation against a real PostgreSQL DB.
/// Covers: happy path, rollback on failure, race-condition (concurrent checkout), and boundary stock scenarios.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class CreateOrderAtomicDecrementTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static object CheckoutPayload(string? note = null) => new
    {
        Note          = note,
        Email         = "buyer@test.com",
        FullName      = "Nguyen Van A",
        Phone         = "0901234567",
        Province      = "Hà Nội",
        District      = "Ba Đình",
        Ward          = "Phúc Xá",
        StreetAddress = "123 Test Street"
    };

    private async Task<HttpClient> BuildReadyClientAsync(Guid productId, int quantity = 1)
    {
        // Use direct DB seeding to bypass the 3-req/min registration rate limit.
        var client = await fixture.CreateAuthenticatedClientDirectAsync();
        var r = await client.PostAsJsonAsync("/api/v1/basket/items",
            new { ProductId = productId, Quantity = quantity });
        r.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<int> GetStockAsync(Guid productId)
        => (await fixture.GetProductAsync(productId)).Stock;

    private async Task<List<Order>> GetOrdersAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        return await db.Orders.Include(o => o.Items).ToListAsync();
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_ValidBasket_Returns201_DeductsStock_PersistsOrder()
    {
        // Arrange
        var productId = await fixture.SeedProductAsync(stock: 10, price: 25.00m);
        var client    = await BuildReadyClientAsync(productId, quantity: 3);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload("Integration test"));

        // Assert — HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert — stock deducted atomically
        (await GetStockAsync(productId)).Should().Be(7, "3 units were purchased from stock of 10");

        // Assert — order persisted with correct total
        var orders = await GetOrdersAsync();
        orders.Should().HaveCount(1);
        orders[0].TotalAmount.Should().Be(25.00m * 3);
        orders[0].Items.Should().HaveCount(1);
        orders[0].Items[0].UnitPrice.Should().Be(25.00m, "price snapshot must be captured at order time");
        orders[0].Items[0].Quantity.Should().Be(3);
    }

    // ─── Rollback: stock exhausted before decrement ───────────────────────────

    [Fact]
    public async Task CreateOrder_StockExactlyZero_Returns400_NoOrderPersisted_StockRemainsZero()
    {
        // Arrange: seed with stock=1 so basket add succeeds, then drain to 0 before checkout.
        // This simulates another user buying the last unit between basket-add and checkout.
        var productId = await fixture.SeedProductAsync(stock: 1);
        var client    = await BuildReadyClientAsync(productId, quantity: 1); // basket add succeeds (stock=1)

        // Drain stock to 0 directly in DB (simulates concurrent buyer)
        using (var scope = fixture.Services.CreateScope())
        {
            var db      = scope.ServiceProvider.GetRequiredService<OrderHub.Infrastructure.Persistence.OrderHubDbContext>();
            var product = await db.Products.FindAsync(productId);
            product!.Stock = 0;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetStockAsync(productId)).Should().Be(0, "stock must remain 0 — atomic decrement guard prevented the order");
        (await fixture.GetOrderCountAsync()).Should().Be(0, "no order must be persisted");
    }

    [Fact]
    public async Task CreateOrder_RequestedQtyExceedsStock_Returns400_StockUnchanged()
    {
        // Arrange: seed stock=2, but basket validator now checks stock too.
        // We use stock=10 for basket add, then reduce stock to 2 in DB before checkout.
        var productId = await fixture.SeedProductAsync(stock: 10);
        var client    = await BuildReadyClientAsync(productId, quantity: 5); // basket: 5 <= 10 ok

        // Reduce stock to 2 in DB after basket add
        using (var scope = fixture.Services.CreateScope())
        {
            var db      = scope.ServiceProvider.GetRequiredService<OrderHub.Infrastructure.Persistence.OrderHubDbContext>();
            var product = await db.Products.FindAsync(productId);
            product!.Stock = 2;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetStockAsync(productId)).Should().Be(2, "stock must not change when atomic decrement guard fires");
        (await fixture.GetOrderCountAsync()).Should().Be(0);
    }

    // ─── Rollback: multi-item order, one item fails ───────────────────────────

    [Fact]
    public async Task CreateOrder_MultiItem_OneItemOutOfStock_Returns400_AllStockRolledBack()
    {
        // Arrange — productA has plenty of stock; productB has stock=1 initially so basket add succeeds
        var productAId = await fixture.SeedProductAsync(stock: 10, price: 10m, name: "Product A");
        var productBId = await fixture.SeedProductAsync(stock: 1,  price: 20m, name: "Product B");

        var client = await fixture.CreateAuthenticatedClientDirectAsync();
        // Add both products to basket
        (await client.PostAsJsonAsync("/api/v1/basket/items",
            new { ProductId = productAId, Quantity = 2 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/basket/items",
            new { ProductId = productBId, Quantity = 1 })).EnsureSuccessStatusCode();

        // Drain productB stock to 0 in DB before checkout to simulate it going out of stock
        using (var scope = fixture.Services.CreateScope())
        {
            var db      = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var product = await db.Products.FindAsync(productBId);
            product!.Stock = 0;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload());

        // Assert — must fail and rollback everything
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // CRITICAL: productA stock must be rolled back to 10 (not 8)
        (await GetStockAsync(productAId)).Should().Be(10,
            "productA decrement must be rolled back when productB fails");
        (await GetStockAsync(productBId)).Should().Be(0);
        (await fixture.GetOrderCountAsync()).Should().Be(0, "no order must be persisted on partial failure");
    }

    // ─── Rollback: boundary — exactly 1 unit available ───────────────────────

    [Fact]
    public async Task CreateOrder_StockEqualsRequestedQty_Succeeds_StockBecomesZero()
    {
        // Arrange
        var productId = await fixture.SeedProductAsync(stock: 3, price: 10m);
        var client    = await BuildReadyClientAsync(productId, quantity: 3);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload());

        // Assert — exact match must succeed
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await GetStockAsync(productId)).Should().Be(0, "stock must reach exactly 0");
        (await fixture.GetOrderCountAsync()).Should().Be(1);
    }

    // ─── Race condition / concurrency ─────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_20ConcurrentRequests_Stock5_Exactly5SucceedAndStockIsZero()
    {
        // Arrange
        const int totalRequests = 20;
        const int stock         = 5;
        var productId = await fixture.SeedProductAsync(stock);

        var clients = new List<HttpClient>();
        for (var i = 0; i < totalRequests; i++)
            clients.Add(await BuildReadyClientAsync(productId, quantity: 1));

        // Act — all 20 users checkout simultaneously
        var responses = await Task.WhenAll(
            clients.Select(c => c.PostAsJsonAsync("/api/v1/orders", CheckoutPayload())));

        // Assert — counts
        var successes = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failures  = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        successes.Should().Be(stock,
            "exactly {0} orders should succeed when stock is {0}", stock);
        failures.Should().Be(totalRequests - stock,
            "{0} requests should fail with insufficient stock", totalRequests - stock);

        // Assert — no unexpected status codes
        responses
            .Where(r => r.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.BadRequest))
            .Should().BeEmpty("all responses must be 201 or 400");

        // Assert — final state
        (await GetStockAsync(productId)).Should().Be(0,    "all stock must be consumed exactly");
        (await fixture.GetOrderCountAsync()).Should().Be(stock, "exactly {0} orders must be persisted", stock);
    }

    [Fact]
    public async Task CreateOrder_50ConcurrentRequests_Stock10_Exactly10SucceedAndStockIsZero()
    {
        // Arrange — heavier concurrency stress test
        const int totalRequests = 50;
        const int stock         = 10;
        var productId = await fixture.SeedProductAsync(stock);

        var clients = new List<HttpClient>();
        for (var i = 0; i < totalRequests; i++)
            clients.Add(await BuildReadyClientAsync(productId, quantity: 1));

        // Act
        var responses = await Task.WhenAll(
            clients.Select(c => c.PostAsJsonAsync("/api/v1/orders", CheckoutPayload())));

        // Assert
        responses.Count(r => r.StatusCode == HttpStatusCode.Created)
            .Should().Be(stock, "exactly {0} succeed when stock={0}", stock);

        responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest)
            .Should().Be(totalRequests - stock);

        (await GetStockAsync(productId)).Should().Be(0);
        (await fixture.GetOrderCountAsync()).Should().Be(stock);
    }

    // ─── Idempotency of failure: basket preserved on failure ─────────────────

    [Fact]
    public async Task CreateOrder_FailedOrder_BasketIsPreserved_UserCanRetryOrModify()
    {
        // Arrange — seed with stock=1 so basket add succeeds, then drain to 0 before checkout.
        var productId = await fixture.SeedProductAsync(stock: 1);
        var client    = await BuildReadyClientAsync(productId, quantity: 1);

        // Drain stock to 0 directly in DB
        using (var scope = fixture.Services.CreateScope())
        {
            var db      = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var product = await db.Products.FindAsync(productId);
            product!.Stock = 0;
            await db.SaveChangesAsync();
        }

        // Act
        var failedResponse = await client.PostAsJsonAsync("/api/v1/orders", CheckoutPayload());
        failedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Assert — basket still exists so user can retry after restocking
        var basketResponse = await client.GetAsync("/api/v1/basket");
        basketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var basket = await basketResponse.Content.ReadFromJsonAsync<BasketDto>();
        basket!.Items.Should().HaveCount(1, "basket must be preserved when order fails");
    }

    // ─── Private DTO for basket deserialization ───────────────────────────────
    private record BasketDto(Guid UserId, List<BasketItemDto> Items);
    private record BasketItemDto(Guid ProductId, int Quantity);
}
