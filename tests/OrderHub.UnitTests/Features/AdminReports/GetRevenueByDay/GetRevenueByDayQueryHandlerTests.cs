using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Features.AdminReports;
using OrderHub.Application.Features.AdminReports.GetRevenueByDay;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.AdminReports.GetRevenueByDay;

public class GetRevenueByDayQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;

    public GetRevenueByDayQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _orderRepositoryMock = new Mock<IOrderRepository>();
    }

    private GetRevenueByDayQueryHandler CreateHandler()
    {
        var cache = MockHelpers.CreateMemoryCache();
        return new GetRevenueByDayQueryHandler(_orderRepositoryMock.Object, cache);
    }

    [Fact]
    public async Task Handle_ReturnsRevenueData()
    {
        // Arrange
        var data = new List<RevenueByDay>
        {
            new(new DateTime(2025, 6, 1), 12, 3400.00m),
            new(new DateTime(2025, 6, 2), 8, 2100.50m),
            new(new DateTime(2025, 6, 3), 15, 4200.75m),
        };

        _orderRepositoryMock
            .Setup(r => r.GetRevenueByDayAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var query = new GetRevenueByDayQuery();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Date.Should().Be(new DateTime(2025, 6, 1));
        result.Value[0].OrderCount.Should().Be(12);
        result.Value[0].TotalRevenue.Should().Be(3400.00m);
        result.Value[1].Date.Should().Be(new DateTime(2025, 6, 2));
        result.Value[1].OrderCount.Should().Be(8);
        result.Value[2].Date.Should().Be(new DateTime(2025, 6, 3));
        result.Value[2].TotalRevenue.Should().Be(4200.75m);
    }

    [Fact]
    public async Task Handle_WithDateFilters_PassesToRepository()
    {
        // Arrange
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);

        _orderRepositoryMock
            .Setup(r => r.GetRevenueByDayAsync(
                from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RevenueByDay>());

        var query = new GetRevenueByDayQuery(From: from, To: to);
        var handler = CreateHandler();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(
            r => r.GetRevenueByDayAsync(from, to, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SecondCall_UsesCache()
    {
        // Arrange
        var data = new List<RevenueByDay>
        {
            new(new DateTime(2025, 6, 1), 12, 3400.00m),
        };

        _orderRepositoryMock
            .Setup(r => r.GetRevenueByDayAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var query = new GetRevenueByDayQuery();
        var handler = CreateHandler();

        // Act
        var firstResult = await handler.Handle(query, CancellationToken.None);
        var secondResult = await handler.Handle(query, CancellationToken.None);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().HaveCount(1);

        _orderRepositoryMock.Verify(
            r => r.GetRevenueByDayAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
