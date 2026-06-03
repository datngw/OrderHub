using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Orders.DeleteOrder;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Orders.DeleteOrder;

public class DeleteOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly DeleteOrderCommandHandler _handler;

    public DeleteOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        var logger = Mock.Of<ILogger<DeleteOrderCommandHandler>>();
        _handler = new DeleteOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    [Fact]
    public async Task Handle_ActiveOrder_SoftDeletes()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Status = OrderStatusEnum.Confirmed,
            TotalAmount = 99.99m,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var command = new DeleteOrderCommand(orderId);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.IsDeleted.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ReturnsSuccessIdempotent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Status = OrderStatusEnum.Confirmed,
            TotalAmount = 99.99m,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };

        var command = new DeleteOrderCommand(orderId);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.IsDeleted.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new DeleteOrderCommand(orderId);

        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NotFoundById(orderId));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
