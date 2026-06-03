using FluentAssertions;
using Moq;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets.RemoveBasketItem;
using OrderHub.Domain.Baskets;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Baskets.RemoveBasketItem;

public class RemoveBasketItemCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly RemoveBasketItemCommandHandler _handler;

    public RemoveBasketItemCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new RemoveBasketItemCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_RemoveItem_ReturnsUpdatedBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = productId1, ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 2 },
                new BasketItem { ProductId = productId2, ProductName = "Gadget", SKU = "SKU-002", UnitPrice = 19.99m, Quantity = 1 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new RemoveBasketItemCommand(productId1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ProductId.Should().Be(productId2);

        _basketRepositoryMock.Verify(r => r.SetAsync(basket, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveLastItem_RemovesEntireBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

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

        var command = new RemoveBasketItemCommand(productId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalAmount.Should().Be(0);

        _basketRepositoryMock.Verify(r => r.RemoveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BasketNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        var command = new RemoveBasketItemCommand(Guid.NewGuid());

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
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem { ProductId = Guid.NewGuid(), ProductName = "Widget", SKU = "SKU-001", UnitPrice = 9.99m, Quantity = 2 }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var command = new RemoveBasketItemCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Baskets.ItemNotFound");
    }
}
