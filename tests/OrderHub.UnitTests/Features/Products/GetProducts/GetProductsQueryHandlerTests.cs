using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.GetProducts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Products.GetProducts;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;

    public GetProductsQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
    }

    private GetProductsQueryHandler CreateHandler()
    {
        // Fresh cache per test to avoid cross-test contamination
        var cache = MockHelpers.CreateMemoryCache();
        return new GetProductsQueryHandler(_productRepositoryMock.Object, cache);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResults()
    {
        // Arrange
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SKU = "SKU-001",
                Name = "Widget A",
                Description = "First widget",
                Price = 10.00m,
                Stock = 50,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                SKU = "SKU-002",
                Name = "Widget B",
                Description = "Second widget",
                Price = 20.00m,
                Stock = 30,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        const int totalCount = 25;

        _productRepositoryMock
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        var query = new GetProductsQuery(Page: 1, PageSize: 20);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeTrue();
    }
}
