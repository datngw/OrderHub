using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.GetMyOrders;

public record GetMyOrdersQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null,
    DateTime? FromDate = null, DateTime? ToDate = null,
    string? SortBy = "CreatedAt", string? SortOrder = "desc")
    : IQuery<PagedResult<OrderResponse>>;
