using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets.ClearBasket;
using OrderHub.Domain.Baskets;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Baskets.ClearBasket;

public class ClearBasketCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly ClearBasketCommandHandler _handler;

    public ClearBasketCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _basketRepositoryMock = new Mock<IBasketRepository>();
        _userContextMock = new Mock<IUserContext>();
        _handler = new ClearBasketCommandHandler(
            _userContextMock.Object,
            _basketRepositoryMock.Object,
            Mock.Of<ILogger<ClearBasketCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ExistingBasket_ClearsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var command = new ClearBasketCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _basketRepositoryMock.Verify(r => r.RemoveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoBasket_StillReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserId).Returns(userId);

        var command = new ClearBasketCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _basketRepositoryMock.Verify(r => r.RemoveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
