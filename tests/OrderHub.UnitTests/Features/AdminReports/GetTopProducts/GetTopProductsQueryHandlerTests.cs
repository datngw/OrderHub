using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Features.AdminReports;
using OrderHub.Application.Features.AdminReports.GetTopProducts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.AdminReports.GetTopProducts;

public class GetTopProductsQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;

    public GetTopProductsQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _orderRepositoryMock = new Mock<IOrderRepository>();
    }

    private GetTopProductsQueryHandler CreateHandler()
    {
        var cache = MockHelpers.CreateMemoryCache();
        return new GetTopProductsQueryHandler(_orderRepositoryMock.Object, cache);
    }

    [Fact]
    public async Task Handle_ReturnsTopProducts()
    {
        // Arrange
        var data = new List<TopProductRevenue>
        {
            new(Guid.NewGuid(), "Widget A", 100, 5000.00m),
            new(Guid.NewGuid(), "Widget B", 75, 3750.00m),
            new(Guid.NewGuid(), "Widget C", 50, 2500.00m),
        };

        _orderRepositoryMock
            .Setup(r => r.GetTopProductsByRevenueAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var query = new GetTopProductsQuery();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].ProductName.Should().Be("Widget A");
        result.Value[0].TotalQuantity.Should().Be(100);
        result.Value[0].TotalRevenue.Should().Be(5000.00m);
        result.Value[1].ProductName.Should().Be("Widget B");
        result.Value[2].ProductName.Should().Be("Widget C");
    }

    [Fact]
    public async Task Handle_WithDateFilters_PassesToRepository()
    {
        // Arrange
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);

        _orderRepositoryMock
            .Setup(r => r.GetTopProductsByRevenueAsync(
                from, to, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopProductRevenue>());

        var query = new GetTopProductsQuery(From: from, To: to, Top: 5);
        var handler = CreateHandler();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(
            r => r.GetTopProductsByRevenueAsync(from, to, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SecondCall_UsesCache()
    {
        // Arrange
        var data = new List<TopProductRevenue>
        {
            new(Guid.NewGuid(), "Widget A", 100, 5000.00m),
        };

        _orderRepositoryMock
            .Setup(r => r.GetTopProductsByRevenueAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var query = new GetTopProductsQuery();
        var handler = CreateHandler();

        // Act
        var firstResult = await handler.Handle(query, CancellationToken.None);
        var secondResult = await handler.Handle(query, CancellationToken.None);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().HaveCount(1);

        _orderRepositoryMock.Verify(
            r => r.GetTopProductsByRevenueAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
