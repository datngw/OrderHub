using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.GetProductById;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Products.GetProductById;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;

    public GetProductByIdQueryHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
    }

    private GetProductByIdQueryHandler CreateHandler()
    {
        // Fresh cache per test to avoid cross-test contamination
        var cache = MockHelpers.CreateMemoryCache();
        return new GetProductByIdQueryHandler(_productRepositoryMock.Object, cache);
    }

    [Fact]
    public async Task Handle_ActiveProduct_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            SKU = "SKU-001",
            Name = "Widget",
            Description = "A useful widget",
            Price = 9.99m,
            Stock = 100,
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var query = new GetProductByIdQuery(productId);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(productId);
        result.Value.SKU.Should().Be("SKU-001");
        result.Value.Name.Should().Be("Widget");
        result.Value.Price.Should().Be(9.99m);
        result.Value.Stock.Should().Be(100);
        result.Value.Category.Should().Be("Electronics");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFoundById(productId));
    }

    [Fact]
    public async Task Handle_InactiveProduct_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            SKU = "SKU-001",
            Name = "Widget",
            Description = "A widget",
            Price = 9.99m,
            Stock = 100,
            Category = "Electronics",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var query = new GetProductByIdQuery(productId);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFoundById(productId));
    }
}
