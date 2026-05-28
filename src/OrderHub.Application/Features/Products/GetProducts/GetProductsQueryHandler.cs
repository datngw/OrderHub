using Mapster;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository, IMemoryCache cache)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var version = cache.GetProductVersion();
        var cacheKey = CacheKeys.Products.List(
            version, request.Page, request.PageSize, request.Category,
            request.MinPrice, request.MaxPrice, request.Search,
            request.SortBy, request.SortOrder);

        var cached = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(30))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSize(1);

            var (items, totalCount) = await productRepository.GetFilteredAsync(
                request.Category, request.MinPrice, request.MaxPrice,
                request.Search, request.SortBy, request.SortOrder,
                request.Page, request.PageSize, cancellationToken);

            return new PagedResult<ProductResponse>
            {
                Items = items.Adapt<List<ProductResponse>>(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        });

        return Result<PagedResult<ProductResponse>>.Success(cached!);
    }
}
