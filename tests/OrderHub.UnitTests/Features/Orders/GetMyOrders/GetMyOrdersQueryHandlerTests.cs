using Moq;
using FluentAssertions;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.GetMyOrders;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Orders.GetMyOrders;

public class GetMyOrdersQueryHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetMyOrdersQueryHandler _handler;

    public GetMyOrdersQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _orderRepositoryMock = new Mock<IOrderRepository>();

        _handler = new GetMyOrdersQueryHandler(
            _userContextMock.Object,
            _orderRepositoryMock.Object);
    }

    private Order CreateOrder(Guid userId, OrderStatusEnum status, decimal totalAmount, Product product)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            TotalAmount = totalAmount,
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = (int)(totalAmount / product.Price),
                    UnitPrice = product.Price,
                    Product = product
                }
            ]
        };
    }

    [Fact]
    public async Task Handle_ReturnsPagedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 10m };

        var orders = new List<Order>
        {
            CreateOrder(userId, OrderStatusEnum.Pending, 20m, product),
            CreateOrder(userId, OrderStatusEnum.Confirmed, 30m, product)
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(
                userId, 1, 20,
                null, null, null,
                "CreatedAt", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 2));

        var query = new GetMyOrdersQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);

        result.Value.Items[0].UserId.Should().Be(userId);
        result.Value.Items[0].Status.Should().Be("Pending");
        result.Value.Items[0].TotalAmount.Should().Be(20m);
        result.Value.Items[0].Items.Should().HaveCount(1);

        result.Value.Items[1].UserId.Should().Be(userId);
        result.Value.Items[1].Status.Should().Be("Confirmed");
        result.Value.Items[1].TotalAmount.Should().Be(30m);
        result.Value.Items[1].Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 10m };

        var pendingOrders = new List<Order>
        {
            CreateOrder(userId, OrderStatusEnum.Pending, 20m, product)
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(
                userId, 1, 20,
                "Pending", null, null,
                "CreatedAt", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((pendingOrders, 1));

        var query = new GetMyOrdersQuery(Page: 1, PageSize: 20, Status: "Pending");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be("Pending");

        _orderRepositoryMock.Verify(
            r => r.GetByUserIdAsync(
                userId, 1, 20,
                "Pending", null, null,
                "CreatedAt", "desc",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SortsByTotalAmountAscending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 10m };

        var sortedOrders = new List<Order>
        {
            CreateOrder(userId, OrderStatusEnum.Pending, 10m, product),
            CreateOrder(userId, OrderStatusEnum.Confirmed, 30m, product)
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(
                userId, 1, 20,
                null, null, null,
                "TotalAmount", "asc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((sortedOrders, 2));

        var query = new GetMyOrdersQuery(Page: 1, PageSize: 20, SortBy: "TotalAmount", SortOrder: "asc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        _orderRepositoryMock.Verify(
            r => r.GetByUserIdAsync(
                userId, 1, 20,
                null, null, null,
                "TotalAmount", "asc",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FiltersByDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 10m };
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 6, 1);

        var filteredOrders = new List<Order>
        {
            CreateOrder(userId, OrderStatusEnum.Shipped, 50m, product)
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(
                userId, 1, 20,
                null, fromDate, toDate,
                "CreatedAt", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((filteredOrders, 1));

        var query = new GetMyOrdersQuery(Page: 1, PageSize: 20, FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);

        _orderRepositoryMock.Verify(
            r => r.GetByUserIdAsync(
                userId, 1, 20,
                null, fromDate, toDate,
                "CreatedAt", "desc",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CombinesAllFilters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 10m };
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 12, 31);

        var orders = new List<Order>
        {
            CreateOrder(userId, OrderStatusEnum.Delivered, 40m, product)
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(
                userId, 1, 10,
                "Delivered", fromDate, toDate,
                "TotalAmount", "asc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 1));

        var query = new GetMyOrdersQuery(
            Page: 1, PageSize: 10,
            Status: "Delivered",
            FromDate: fromDate, ToDate: toDate,
            SortBy: "TotalAmount", SortOrder: "asc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be("Delivered");
    }
}
