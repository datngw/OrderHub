using FluentAssertions;
using Moq;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets.AddBasketItem;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Baskets.AddBasketItem;

public class AddBasketItemCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly AddBasketItemCommandHandler _handler;

    public AddBasketItemCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new AddBasketItemCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object,
            _productRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NewItem_AddsToBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            SKU = "SKU-001",
            Price = 9.99m,
            Stock = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        var command = new AddBasketItemCommand(productId, 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ProductId.Should().Be(productId);
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.Items[0].UnitPrice.Should().Be(9.99m);

        _basketRepositoryMock.Verify(r => r.SetAsync(It.IsAny<Basket>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingItem_MergesQuantity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            SKU = "SKU-001",
            Price = 9.99m,
            Stock = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = productId, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 3 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new AddBasketItemCommand(productId, 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new AddBasketItemCommand(productId, 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.ProductNotFound");
    }

    [Fact]
    public async Task Handle_ProductDeleted_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            SKU = "SKU-001",
            Price = 9.99m,
            Stock = 100,
            IsDeleted = true
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new AddBasketItemCommand(productId, 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.ProductUnavailable");
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            SKU = "SKU-001",
            Price = 9.99m,
            Stock = 5
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new AddBasketItemCommand(productId, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.InsufficientStock");
    }

    [Fact]
    public async Task Handle_MergeExceedsStock_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            SKU = "SKU-001",
            Price = 9.99m,
            Stock = 10
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = productId, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 8 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new AddBasketItemCommand(productId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.InsufficientStock");
    }
}
