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

        _userContextMock         = new Mock<IUserContext>();
        _basketRepositoryMock    = new Mock<IBasketRepository>();
        _orderRepositoryMock     = new Mock<IOrderRepository>();
        _productRepositoryMock   = new Mock<IProductRepository>();
        _unitOfWorkMock          = new Mock<IUnitOfWork>();
        _cache                   = MockHelpers.CreateMemoryCache();
        var logger               = Mock.Of<ILogger<CreateOrderCommandHandler>>();

        // ExecuteInTransactionAsync passes the action through directly (no real DB transaction in unit tests)
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<OrderResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result<OrderResponse>>>, CancellationToken>(
                (action, ct) => action(ct));

        _handler = new CreateOrderCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ValidBasket_SingleItem_CreatesOrderAndClearsBasket()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 100, price: 9.99m);
        SetupUser(userId);
        SetupBasket(userId, (product, 2));
        SetupGetByIds(product);
        SetupDecrement(product.Id, quantity: 2, affected: 1);

        // Act
        var result = await _handler.Handle(MakeCommand(note: "Test order"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be("Pending");
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalAmount.Should().Be(9.99m * 2);

        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _basketRepositoryMock.Verify(r => r.RemoveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidBasket_MultiItem_CalculatesTotalCorrectly()
    {
        // Arrange
        var userId   = Guid.NewGuid();
        var productA = MakeProduct(stock: 10, price: 5.00m);
        var productB = MakeProduct(stock: 20, price: 12.50m);
        SetupUser(userId);
        SetupBasket(userId, (productA, 3), (productB, 2));
        SetupGetByIds(productA, productB);
        SetupDecrement(productA.Id, quantity: 3, affected: 1);
        SetupDecrement(productB.Id, quantity: 2, affected: 1);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be((5.00m * 3) + (12.50m * 2));
        result.Value.Items.Should().HaveCount(2);

        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Error / validation paths
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_EmptyBasket_ReturnsEmptyOrderError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId);
        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.EmptyOrder");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFoundInCatalog_ReturnsProductNotFoundError()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 10, price: 10m);
        SetupUser(userId);
        SetupBasket(userId, (product, 2));

        // Catalog does not contain product (returns empty list)
        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.ProductNotFound");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletedProduct_ReturnsProductUnavailableError()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 10, price: 10m);
        product.IsDeleted = true;
        SetupUser(userId);
        SetupBasket(userId, (product, 2));
        SetupGetByIds(product);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.ProductUnavailable");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveProduct_ReturnsProductUnavailableError()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 10, price: 10m);
        product.IsActive = false;
        SetupUser(userId);
        SetupBasket(userId, (product, 2));
        SetupGetByIds(product);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.ProductUnavailable");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InsufficientStockInPreCheck_ReturnsInsufficientStockError()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 5, price: 10m);
        SetupUser(userId);
        SetupBasket(userId, (product, 10)); // requests 10, only 5 in stock
        SetupGetByIds(product);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.InsufficientStock");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Atomic Decrement — race condition: stock was enough at pre-check but
    // exhausted by the time the UPDATE runs inside the transaction
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_DecrementReturnsZero_RaceCondition_ReturnsInsufficientStockError()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 1, price: 10m);  // pre-check passes (1 >= 1)
        SetupUser(userId);
        SetupBasket(userId, (product, 1));
        SetupGetByIds(product);
        // But by the time the atomic UPDATE runs, another request already took the last unit
        SetupDecrement(product.Id, 1, affected: 0);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert — race condition caught by atomic UPDATE
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.InsufficientStock");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        _basketRepositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultiItemOrder_FirstDecrementSucceeds_SecondFails_NeitherOrderNorBasketClearPersisted()
    {
        // Arrange — simulates partial failure: productA ok, productB fails (race condition)
        var userId   = Guid.NewGuid();
        var productA = MakeProduct(stock: 10, price: 5m);
        var productB = MakeProduct(stock: 1, price: 8m);
        SetupUser(userId);
        SetupBasket(userId, (productA, 2), (productB, 1));
        SetupGetByIds(productA, productB);
        SetupDecrement(productA.Id, 2, affected: 1);   // first item succeeds
        SetupDecrement(productB.Id, 1, affected: 0);   // second item fails (race)

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert — whole order must fail; no order persisted; basket must NOT be cleared
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.InsufficientStock");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        _basketRepositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Exception / infrastructure failure path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_OrderRepositoryThrows_ExceptionPropagates()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 10, price: 10m);
        SetupUser(userId);
        SetupBasket(userId, (product, 1));
        SetupGetByIds(product);
        SetupDecrement(product.Id, 1, affected: 1);

        _orderRepositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Throws(new InvalidOperationException("DB write error"));

        // Act
        var act = () => _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert — exception must propagate so ExecuteInTransactionAsync can rollback
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB write error");
    }

    // -------------------------------------------------------------------------
    // Cache invalidation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SuccessfulOrder_InvalidatesProductAndReportCaches()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var product = MakeProduct(stock: 10, price: 10m);
        SetupUser(userId);
        SetupBasket(userId, (product, 1));
        SetupGetByIds(product);
        SetupDecrement(product.Id, 1, affected: 1);

        // Seed cache entries with required Size (MockHelpers.CreateMemoryCache sets SizeLimit=1000)
        var opts = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions().SetSize(1);
        _cache.Set("reports:version", "old-version", opts);
        _cache.Set("products:version", "old-p-version", opts);

        // Act
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cache.TryGetValue("reports:version", out _).Should().BeFalse("report cache must be invalidated");
        _cache.TryGetValue("products:version", out _).Should().BeFalse("product cache must be invalidated");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetupUser(Guid userId) =>
        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

    private void SetupGetByIds(params Product[] products) =>
        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());

    private void SetupDecrement(Guid productId, int quantity, int affected) =>
        _productRepositoryMock
            .Setup(r => r.TryDecrementStockAsync(productId, quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(affected);

    private void SetupBasket(Guid userId, params (Product Product, int Quantity)[] items)
    {
        var basket = new Basket
        {
            UserId = userId,
            Items  = items.Select(i => new BasketItem
            {
                ProductId   = i.Product.Id,
                ProductName = i.Product.Name,
                SKU         = i.Product.SKU,
                UnitPrice   = i.Product.Price,
                Quantity    = i.Quantity
            }).ToList()
        };
        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
    }

    private static Product MakeProduct(int stock, decimal price) => new()
    {
        Id          = Guid.NewGuid(),
        SKU         = $"SKU-{Guid.NewGuid():N}"[..12],
        Name        = $"Product-{Guid.NewGuid():N}"[..12],
        Description = "Test product",
        Price       = price,
        Stock       = stock,
        Category    = "Test",
        IsActive    = true
    };

    private static CreateOrderCommand MakeCommand(string? note = null) =>
        new(note, "test@example.com", "Nguyen Van A", "0901234567",
            "Hà Nội", "Quận Ba Đình", "Phường Phúc Xá", "123 Đường Test");
}
