using FluentAssertions;
using Moq;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets.UpdateBasketItem;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Baskets.UpdateBasketItem;

public class UpdateBasketItemCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly UpdateBasketItemCommandHandler _handler;

    public UpdateBasketItemCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new UpdateBasketItemCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object,
            _productRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var product = new Product
        {
            Id = productId,
            Name = "Widget Updated",
            SKU = "SKU-001",
            Price = 19.99m,
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
                new BasketItem { ProductId = productId, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 2 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new UpdateBasketItemCommand(productId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Quantity.Should().Be(5);
        result.Value.Items[0].UnitPrice.Should().Be(19.99m);

        _basketRepositoryMock.Verify(r => r.SetAsync(basket, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BasketNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        var command = new UpdateBasketItemCommand(productId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.NotFound");
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = otherProductId, ProductName = "Other", SKU = "SKU-002", UnitPrice = 5m, Quantity = 1 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new UpdateBasketItemCommand(productId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.ItemNotFound");
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure()
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
            Stock = 3
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = productId, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 2 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new UpdateBasketItemCommand(productId, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.InsufficientStock");
    }
}
