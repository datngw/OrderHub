using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderHub.IntegrationTests.Shared;

namespace OrderHub.IntegrationTests.Features.Orders;

[Collection(nameof(IntegrationTestCollection))]
public class CreateOrderConcurrencyTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrder_With20ConcurrentUsersAndStock5_Exactly5Succeed()
    {
        // Arrange
        const int totalRequests = 20;
        const int stock = 5;
        var productId = await fixture.SeedProductAsync(stock);

        // Create 20 authenticated customers, each with the product in their basket
        var clients = new List<HttpClient>();
        for (var i = 0; i < totalRequests; i++)
        {
            var client = await fixture.CreateAuthenticatedCustomerAsync();
            var addBasketResponse = await client.PostAsJsonAsync("/api/v1/basket/items", new
            {
                ProductId = productId,
                Quantity = 1
            });
            addBasketResponse.EnsureSuccessStatusCode();
            clients.Add(client);
        }

        // Act — all 20 users checkout concurrently
        var responses = await Task.WhenAll(clients.Select(client =>
            client.PostAsJsonAsync("/api/v1/orders", new { Note = (string?)null })));

        // Assert — count HTTP status codes
        var successes = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failures = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        successes.Should().Be(stock, "only {0} orders should succeed when stock is {0}", stock);
        failures.Should().Be(totalRequests - stock,
            "{0} requests should fail with insufficient stock", totalRequests - stock);

        // Assert — no unexpected status codes
        var unexpected = responses
            .Where(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.BadRequest)
            .ToList();
        unexpected.Should().BeEmpty("all responses should be 201 or 400, got: {0}",
            string.Join(", ", unexpected.Select(r => r.StatusCode)));

        // Assert — final stock is exactly 0
        var product = await fixture.GetProductAsync(productId);
        product.Stock.Should().Be(0, "all stock should be consumed");

        // Assert — exactly 5 orders persisted
        var orderCount = await fixture.GetOrderCountAsync();
        orderCount.Should().Be(stock, "exactly {0} orders should be persisted", stock);
    }
}
