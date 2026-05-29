using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderHub.Api.Endpoints.Orders.Requests;
using OrderHub.IntegrationTests.Shared;

namespace OrderHub.IntegrationTests.Features.Orders;

[Collection(nameof(IntegrationTestCollection))]
public class CreateOrderConcurrencyTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrder_With20ConcurrentRequestsAndStock5_Exactly5Succeed()
    {
        // Arrange
        const int totalRequests = 20;
        const int stock = 5;
        var productId = await fixture.SeedProductAsync(stock);
        var client = await fixture.CreateAuthenticatedCustomerAsync();

        var request = new CreateOrderRequest([new OrderItemRequest(productId, 1)]);

        // Act — fire concurrent order requests
        var responses = await Task.WhenAll(Enumerable.Range(0, totalRequests)
            .Select(_ => client.PostAsJsonAsync("/api/v1/orders", request)));

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
