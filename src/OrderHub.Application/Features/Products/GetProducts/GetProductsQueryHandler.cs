using Mapster;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProducts;

public sealed class GetProductsQueryHandler(DbContext dbContext)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Product>().AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);
        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search));

        query = ApplySorting(query, request.SortBy, request.SortOrder);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProductResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, string? sortOrder)
    {
        var isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => isDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "category" => isDesc ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "sku" => isDesc ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
            _ => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };
    }
}
