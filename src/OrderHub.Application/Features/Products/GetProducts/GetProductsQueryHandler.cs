using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await productRepository.GetFilteredAsync(
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            request.SortBy,
            request.SortOrder,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<ProductResponse>>.Success(new PagedResult<ProductResponse>
        {
            Items = items.Adapt<List<ProductResponse>>(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
