using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Orders.UpdateOrderStatus;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Orders.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly UpdateOrderStatusCommandHandler _handler;

    public UpdateOrderStatusCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _orderRepositoryMock = new Mock<IOrderRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        var logger = Mock.Of<ILogger<UpdateOrderStatusCommandHandler>>();

        _handler = new UpdateOrderStatusCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    [Fact]
    public async Task Handle_PendingToConfirmed_ReturnsSuccess()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Pending,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Confirmed);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatusEnum.Confirmed);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmedToShipped_ReturnsSuccess()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Confirmed,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatusEnum.Shipped);
    }

    [Fact]
    public async Task Handle_ShippedToDelivered_ReturnsSuccess()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Shipped,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Delivered);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatusEnum.Delivered);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new UpdateOrderStatusCommand(orderId, OrderStatusEnum.Confirmed);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NotFoundById(orderId));
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Cancelled,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Confirmed);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AlreadyCancelled);
    }

    [Fact]
    public async Task Handle_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Pending,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Pending → Shipped is invalid (should go Pending → Confirmed)
        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Shipped);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition(OrderStatusEnum.Pending, OrderStatusEnum.Shipped));
    }

    [Fact]
    public async Task Handle_NoTransitionFromDelivered_ReturnsFailure()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Delivered,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Pending);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition(OrderStatusEnum.Delivered, OrderStatusEnum.Pending));
    }

    [Fact]
    public async Task Handle_NoTransitionFromCancelledToPending_ReturnsFailure()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatusEnum.Cancelled,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            Items = []
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand(order.Id, OrderStatusEnum.Pending);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Cancelled is caught first by the AlreadyCancelled check
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AlreadyCancelled);
    }
}
