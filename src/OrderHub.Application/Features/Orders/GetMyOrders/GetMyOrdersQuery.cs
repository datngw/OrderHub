using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.GetMyOrders;

public record GetMyOrdersQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<OrderResponse>>;
