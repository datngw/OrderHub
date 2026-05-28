using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersQueryHandler(
    IUserContext userContext,
    IOrderRepository orderRepository)
    : IQueryHandler<GetMyOrdersQuery, PagedResult<OrderResponse>>
{
    public async Task<Result<PagedResult<OrderResponse>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orderRepository.GetByUserIdAsync(
            userContext.UserId, request.Page, request.PageSize, cancellationToken);

        return new PagedResult<OrderResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Select(i => new OrderItemResponse(
                i.Id,
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.UnitPrice,
                i.UnitPrice * i.Quantity)).ToList(),
            order.CreatedAt,
            order.UpdatedAt);
    }
}
