using FluentAssertions;
using Moq;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets;
using OrderHub.Application.Features.Baskets.GetBasket;
using OrderHub.Domain.Baskets;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Baskets.GetBasket;

public class GetBasketQueryHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly GetBasketQueryHandler _handler;

    public GetBasketQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new GetBasketQueryHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingBasket_ReturnsBasketResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var basket = new Basket
        {
            UserId = userId,
            Items =
            [
                new BasketItem
                {
                    ProductId = Guid.NewGuid(), ProductName = "Widget", SKU = "SKU-001",
                    UnitPrice = 9.99m, Quantity = 2
                }
            ]
        };

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        // Act
        var result = await _handler.Handle(new GetBasketQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalAmount.Should().Be(19.98m);
        result.Value.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoBasket_ReturnsEmptyBasketResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        _basketRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Basket?)null);

        // Act
        var result = await _handler.Handle(new GetBasketQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalAmount.Should().Be(0);
        result.Value.TotalItems.Should().Be(0);
    }
}
