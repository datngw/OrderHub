using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.GetAdminProducts;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Products.GetAdminProducts;

public class GetAdminProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;

    public GetAdminProductsQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
    }

    private GetAdminProductsQueryHandler CreateHandler()
    {
        var cache = MockHelpers.CreateMemoryCache();
        return new GetAdminProductsQueryHandler(_productRepositoryMock.Object, cache, new CacheStampedeGuard());
    }

    [Fact]
    public async Task Handle_ReturnsAllProducts_WhenIsActiveFilterIsNull()
    {
        // Arrange
        var products = new List<ProductListItem>
        {
            new(
                Id: Guid.NewGuid(),
                SKU: "SKU-001",
                Name: "Active Widget",
                Description: "An active widget",
                Price: 10.00m,
                Stock: 50,
                Category: "Electronics",
                IsActive: true,
                CreatedAt: DateTime.UtcNow
            ),
            new(
                Id: Guid.NewGuid(),
                SKU: "SKU-002",
                Name: "Inactive Widget",
                Description: "An inactive widget",
                Price: 20.00m,
                Stock: 0,
                Category: "Electronics",
                IsActive: false,
                CreatedAt: DateTime.UtcNow
            )
        };

        const int totalCount = 2;

        _productRepositoryMock
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        var query = new GetAdminProductsQuery(Page: 1, PageSize: 20, IsActive: null);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveProducts_WhenIsActiveFilterIsTrue()
    {
        // Arrange
        var products = new List<ProductListItem>
        {
            new(
                Id: Guid.NewGuid(),
                SKU: "SKU-001",
                Name: "Active Widget",
                Description: "An active widget",
                Price: 10.00m,
                Stock: 50,
                Category: "Electronics",
                IsActive: true,
                CreatedAt: DateTime.UtcNow
            )
        };

        const int totalCount = 1;

        _productRepositoryMock
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), true,
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        var query = new GetAdminProductsQuery(Page: 1, PageSize: 20, IsActive: true);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyInactiveProducts_WhenIsActiveFilterIsFalse()
    {
        // Arrange
        var products = new List<ProductListItem>
        {
            new(
                Id: Guid.NewGuid(),
                SKU: "SKU-002",
                Name: "Inactive Widget",
                Description: "An inactive widget",
                Price: 20.00m,
                Stock: 0,
                Category: "Electronics",
                IsActive: false,
                CreatedAt: DateTime.UtcNow
            )
        };

        const int totalCount = 1;

        _productRepositoryMock
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), false,
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        var query = new GetAdminProductsQuery(Page: 1, PageSize: 20, IsActive: false);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsActive.Should().BeFalse();
    }
}
