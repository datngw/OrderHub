using Moq;
using FluentAssertions;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.GetOrderById;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Orders.GetOrderById;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _orderRepositoryMock = new Mock<IOrderRepository>();

        _handler = new GetOrderByIdQueryHandler(
            _userContextMock.Object,
            _orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_OwnOrder_ReturnsOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 10m
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatusEnum.Pending,
            TotalAmount = 20m,
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    UnitPrice = 10m,
                    Product = product
                }
            ]
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be("Pending");
        result.Value.TotalAmount.Should().Be(20m);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_AdminAccessingOtherOrder_ReturnsOrder()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Status = OrderStatusEnum.Confirmed,
            TotalAmount = 50m,
            Items = []
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(adminId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(true);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.UserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var query = new GetOrderByIdQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NotFoundById(orderId));
    }

    [Fact]
    public async Task Handle_ForbiddenUser_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Status = OrderStatusEnum.Pending,
            TotalAmount = 10m,
            Items = []
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(otherUserId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.Forbidden);
    }
}
