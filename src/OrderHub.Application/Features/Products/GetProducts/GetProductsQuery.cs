using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Products;

namespace OrderHub.Application.Features.Products.GetProducts;

public record GetProductsQuery(
    int Page = 1, int PageSize = 20,
    string? Category = null, decimal? MinPrice = null, decimal? MaxPrice = null,
    string? Search = null, string? SortBy = "CreatedAt", string? SortOrder = "desc")
    : IQuery<PagedResult<ProductResponse>>;
