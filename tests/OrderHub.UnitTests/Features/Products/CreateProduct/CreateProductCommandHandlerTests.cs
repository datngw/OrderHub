using Mapster;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.CreateProduct;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Products.CreateProduct;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = MockHelpers.CreateMemoryCache();
        _handler = new CreateProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache);
    }

    [Fact]
    public async Task Handle_NewProduct_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateProductCommand(
            "SKU-001", "Widget", "A useful widget", 9.99m, 100, "Electronics");

        _productRepositoryMock
            .Setup(r => r.ExistsBySkuAsync(command.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SKU.Should().Be("SKU-001");
        result.Value.Name.Should().Be("Widget");
        result.Value.Price.Should().Be(9.99m);
        result.Value.Stock.Should().Be(100);
        result.Value.Category.Should().Be("Electronics");

        _productRepositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSku_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProductCommand(
            "SKU-DUP", "Widget", "A useful widget", 9.99m, 100, "Electronics");

        _productRepositoryMock
            .Setup(r => r.ExistsBySkuAsync(command.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.SkuAlreadyExists("SKU-DUP"));

        _productRepositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
