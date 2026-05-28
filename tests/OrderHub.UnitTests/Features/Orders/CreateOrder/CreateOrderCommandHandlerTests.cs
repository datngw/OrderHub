using Mapster;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.CreateOrder;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Orders.CreateOrder;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();

        _handler = new CreateOrderCommandHandler(
            _userContextMock.Object,
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache);
    }

    [Fact]
    public async Task Handle_ValidItems_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1 = CreateProduct(stock: 10, price: 10m);
        var product2 = CreateProduct(stock: 10, price: 20m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(product1.Id, 2),
            new CreateOrderItem(product2.Id, 3)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be("Pending");
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalAmount.Should().Be(10m * 2 + 20m * 3);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(missingProductId, 1)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.ProductNotFound(missingProductId));
    }

    [Fact]
    public async Task Handle_InactiveProduct_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 10m);
        product.IsActive = false;

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(product.Id, 1)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.ProductUnavailable(product.Id));
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 2, price: 10m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(product.Id, 5)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InsufficientStock(product.Name, 5, 2));
    }

    [Fact]
    public async Task Handle_StockDeducted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 15m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(product.Id, 3)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(7);
    }

    [Fact]
    public async Task Handle_Exception_RollsBackTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 10m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);
        _userContextMock.SetupGet(u => u.IsAdmin).Returns(false);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(product.Id, 1)
        ]);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Product CreateProduct(int stock, decimal price)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            SKU = $"SKU-{Guid.NewGuid():N}"[..8],
            Name = $"Product-{Guid.NewGuid():N}"[..8],
            Description = "Test product",
            Price = price,
            Stock = stock,
            Category = "Test",
            IsActive = true
        };
    }
}
