using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.CreateOrder;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Orders.CreateOrder;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        var logger = Mock.Of<ILogger<CreateOrderCommandHandler>>();

        // ExecuteInTransactionAsync passes the action through directly
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<Result<OrderResponse>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result<OrderResponse>>>, CancellationToken>((action, ct) => action(ct));

        _handler = new CreateOrderCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    [Fact]
    public async Task Handle_ValidBasket_CreatesOrderAndClearsBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 100, price: 9.99m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = product.Id, ProductName = product.Name, SKU = product.SKU, UnitPrice = product.Price, Quantity = 2 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand("Test order");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be("Pending");
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalAmount.Should().Be(9.99m * 2);

        product.Stock.Should().Be(98);

        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _basketRepositoryMock.Verify(r => r.RemoveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyBasket_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        var command = new CreateOrderCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.EmptyOrder);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = missingProductId, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 1 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new CreateOrderCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.ProductNotFound(missingProductId));
    }

    [Fact]
    public async Task Handle_DeletedProduct_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 10m);
        product.IsDeleted = true;

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = product.Id, ProductName = product.Name, SKU = product.SKU, UnitPrice = product.Price, Quantity = 1 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand(null);

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

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = product.Id, ProductName = product.Name, SKU = product.SKU, UnitPrice = product.Price, Quantity = 5 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InsufficientStock(product.Name, 5, 2));
        product.Stock.Should().Be(2);
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StockDeducted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 15m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = product.Id, ProductName = product.Name, SKU = product.SKU, UnitPrice = product.Price, Quantity = 3 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var command = new CreateOrderCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(7);
    }

    [Fact]
    public async Task Handle_Exception_ThrownFromHandler()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10, price: 10m);

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = product.Id, ProductName = product.Name, SKU = product.SKU, UnitPrice = product.Price, Quantity = 1 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _productRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        // Simulate exception from the action (e.g., repository throws)
        _orderRepositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Throws(new InvalidOperationException("Database error"));

        var command = new CreateOrderCommand(null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert — exception propagates through ExecuteInTransactionAsync
        await act.Should().ThrowAsync<InvalidOperationException>();
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
            Category = "Test"
        };
    }
}
