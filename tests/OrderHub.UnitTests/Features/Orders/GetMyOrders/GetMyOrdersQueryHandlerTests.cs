using Moq;
using FluentAssertions;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.GetMyOrders;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Orders.GetMyOrders;

public class GetMyOrdersQueryHandlerTests
{
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetMyOrdersQueryHandler _handler;

    public GetMyOrdersQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userContextMock = new Mock<IUserContext>();
        _orderRepositoryMock = new Mock<IOrderRepository>();

        _handler = new GetMyOrdersQueryHandler(
            _userContextMock.Object,
            _orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 10m
        };

        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatusEnum.Pending,
                TotalAmount = 20m,
                Items =
                [
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = 2,
                        UnitPrice = 10m,
                        Product = product
                    }
                ]
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatusEnum.Confirmed,
                TotalAmount = 30m,
                Items =
                [
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = 3,
                        UnitPrice = 10m,
                        Product = product
                    }
                ]
            }
        };

        _userContextMock.SetupGet(u => u.UserId).Returns(userId);

        _orderRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 2));

        var query = new GetMyOrdersQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);

        result.Value.Items[0].UserId.Should().Be(userId);
        result.Value.Items[0].Status.Should().Be("Pending");
        result.Value.Items[0].TotalAmount.Should().Be(20m);
        result.Value.Items[0].Items.Should().HaveCount(1);

        result.Value.Items[1].UserId.Should().Be(userId);
        result.Value.Items[1].Status.Should().Be("Confirmed");
        result.Value.Items[1].TotalAmount.Should().Be(30m);
        result.Value.Items[1].Items.Should().HaveCount(1);
    }
}
