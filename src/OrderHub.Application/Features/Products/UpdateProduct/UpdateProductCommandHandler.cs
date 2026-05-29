using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<UpdateProductCommandHandler> logger)
    : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product update failed: product {ProductId} not found", request.Id);
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));
        }

        request.Adapt(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateProducts(request.Id);

        logger.LogInformation("Product updated: {ProductId}", product.Id);

        return product.Adapt<ProductResponse>();
    }
}
