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
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));

        request.Adapt(product);

        if (request.MainImageUrl is not null)
            product.MainImageUrl = request.MainImageUrl;

        if (request.GalleryImageUrls is not null)
            product.GalleryImageUrls = request.GalleryImageUrls;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateProducts(request.Id);

        logger.LogInformation("Product updated: {ProductId}", product.Id);

        return product.Adapt<ProductResponse>();
    }
}
