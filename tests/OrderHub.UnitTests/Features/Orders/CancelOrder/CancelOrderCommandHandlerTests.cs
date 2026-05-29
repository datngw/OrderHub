using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders.CancelOrder;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Orders.CancelOrder;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        var logger = Mock.Of<ILogger<CancelOrderCommandHandler>>();

        _handler = new CancelOrderCommandHandler(
            _userContextMock.Object,
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    [Fact]
    public async Task Handle_PendingOrder_CancelsAndRestoresStock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "SKU-001",
            Name = "Widget",
            Description = "A widget",
            Price = 10m,
            Stock = 7,
            Category = "Test",
            IsActive = true
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatusEnum.Pending,
            TotalAmount = 30m,
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 3,
                    UnitPrice = 10m,
                    Product = product
                }
            ]
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatusEnum.Cancelled);
        product.Stock.Should().Be(10);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new CancelOrderCommand(orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatusEnum.Cancelled,
            TotalAmount = 10m,
            Items = []
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AlreadyCancelled);
    }

    [Fact]
    public async Task Handle_ConfirmedOrder_CannotBeCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatusEnum.Confirmed,
            TotalAmount = 10m,
            Items = []
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CannotBeCancelled);
    }
}
