using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.AdminReports;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Users;
using OrderHub.Infrastructure.Persistence;
using OrderHub.IntegrationTests.Shared;

namespace OrderHub.IntegrationTests.Features.Reports;

[Collection(nameof(IntegrationTestCollection))]
public class GetReportsTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTopProducts_ReturnsSuccessAndCorrectData()
    {
        // Arrange
        var adminClient = await fixture.CreateAuthenticatedAdminAsync();
        var productId1 = await fixture.SeedProductAsync(stock: 100, price: 10.00m, name: "Product A");
        var productId2 = await fixture.SeedProductAsync(stock: 50, price: 20.00m, name: "Product B");

        // Seed some orders manually to test SQL query
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var adminUser = await db.Users.FirstAsync();

            var order = new Order
            {
                UserId = adminUser.Id,
                Status = OrderStatusEnum.Delivered,
                TotalAmount = 50.00m,
                Items = new List<OrderItem>
                {
                    new() { ProductId = productId1, Quantity = 3, UnitPrice = 10.00m },
                    new() { ProductId = productId2, Quantity = 1, UnitPrice = 20.00m }
                }
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await adminClient.GetAsync("/api/v1/admin/reports/top-products?top=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TopProductRevenueResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var first = result![0];
        first.ProductName.Should().Be("Product A");
        first.TotalQuantity.Should().Be(3);
        first.TotalRevenue.Should().Be(30.00m);

        var second = result[1];
        second.ProductName.Should().Be("Product B");
        second.TotalQuantity.Should().Be(1);
        second.TotalRevenue.Should().Be(20.00m);
    }

    [Fact]
    public async Task GetRevenueByDay_ReturnsSuccessAndCorrectData()
    {
        // Arrange
        var adminClient = await fixture.CreateAuthenticatedAdminAsync();
        var productId = await fixture.SeedProductAsync(stock: 100, price: 15.00m);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var adminUser = await db.Users.FirstAsync();

            var order = new Order
            {
                UserId = adminUser.Id,
                Status = OrderStatusEnum.Delivered,
                TotalAmount = 45.00m,
                Items = new List<OrderItem>
                {
                    new() { ProductId = productId, Quantity = 3, UnitPrice = 15.00m }
                }
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await adminClient.GetAsync("/api/v1/admin/reports/revenue-by-day");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RevenueByDayResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].OrderCount.Should().Be(1);
        result[0].TotalRevenue.Should().Be(45.00m);
    }
}
