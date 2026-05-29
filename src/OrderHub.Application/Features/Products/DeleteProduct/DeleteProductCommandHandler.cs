using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<DeleteProductCommandHandler> logger)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product deletion failed: product {ProductId} not found", request.Id);
            return Result.Failure(ProductErrors.NotFoundById(request.Id));
        }

        if (!product.IsActive)
        {
            logger.LogInformation("Product {ProductId} was already inactive", request.Id);
            return Result.Success();
        }

        product.IsActive = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateProducts(request.Id);

        logger.LogInformation("Product soft-deleted: {ProductId}", request.Id);

        return Result.Success();
    }
}
