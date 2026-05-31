using Mapster;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository,
    IMemoryCache cache,
    CacheStampedeGuard stampedeGuard)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Products.ById(request.Id);

        var cached = await stampedeGuard.GetOrCreateAsync(cache, cacheKey, async entry =>
        {
            var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
            return product is null || !product.IsActive ? null : product.Adapt<ProductResponse>();
        }, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(30))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);
        });

        if (cached is null)
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));

        return Result<ProductResponse>.Success(cached);
    }
}
