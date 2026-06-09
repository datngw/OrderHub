using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.GetOrders;

public record GetOrdersQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null, string? Search = null,
    DateTime? FromDate = null, DateTime? ToDate = null,
    string? SortBy = "CreatedAt", string? SortOrder = "desc")
    : IQuery<PagedResult<OrderResponse>>;
