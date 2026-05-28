using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.UpdateProduct;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Products.UpdateProduct;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        _handler = new UpdateProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsUpdatedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            SKU = "SKU-001",
            Name = "Old Name",
            Description = "Old Description",
            Price = 10.00m,
            Stock = 50,
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var command = new UpdateProductCommand(
            productId, "New Name", "New Description", 19.99m, 75, "Gadgets");

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New Description");
        result.Value.Price.Should().Be(19.99m);
        result.Value.Stock.Should().Be(75);
        result.Value.Category.Should().Be("Gadgets");
        // SKU and IsActive should remain unchanged (mapping config ignores them)
        result.Value.SKU.Should().Be("SKU-001");
        result.Value.IsActive.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            productId, "New Name", "New Description", 19.99m, 75, "Gadgets");

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFoundById(productId));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
