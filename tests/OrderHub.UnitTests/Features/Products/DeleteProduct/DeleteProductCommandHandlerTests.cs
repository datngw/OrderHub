using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.DeleteProduct;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Products.DeleteProduct;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        var logger = Mock.Of<ILogger<DeleteProductCommandHandler>>();
        _handler = new DeleteProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            logger);
    }

    [Fact]
    public async Task Handle_ActiveProduct_SoftDeletes()
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
            Stock = 50,
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var command = new DeleteProductCommand(productId);

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
        product.IsActive.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ReturnsSuccessIdempotent()
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
            Stock = 50,
            Category = "Electronics",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var command = new DeleteProductCommand(productId);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeleteProductCommand(productId);

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
