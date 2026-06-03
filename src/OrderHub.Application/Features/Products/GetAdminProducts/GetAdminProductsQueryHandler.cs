using Mapster;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetAdminProducts;

public sealed class GetAdminProductsQueryHandler(
    IProductRepository productRepository,
    IMemoryCache cache,
    CacheStampedeGuard stampedeGuard)
    : IQueryHandler<GetAdminProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(
        GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var version = cache.GetAdminProductVersion();
        var cacheKey = CacheKeys.AdminProducts.List(
            version, request.Page, request.PageSize, request.Category,
            request.MinPrice, request.MaxPrice, request.Search, request.IsActive,
            request.SortBy, request.SortOrder);

        var cached = await stampedeGuard.GetOrCreateAsync(cache, cacheKey, async entry =>
        {
            var (items, totalCount) = await productRepository.GetFilteredAsync(
                request.Category, request.MinPrice, request.MaxPrice,
                request.Search, request.IsActive,
                request.SortBy, request.SortOrder,
                request.Page, request.PageSize,
                cancellationToken);

            return new PagedResult<ProductResponse>
            {
                Items = items.Adapt<List<ProductResponse>>(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(30))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSize(1);
        });

        return Result<PagedResult<ProductResponse>>.Success(cached!);
    }
}
