using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.GetOrders;

public sealed class GetOrdersQueryHandler(
    IOrderRepository orderRepository)
    : IQueryHandler<GetOrdersQuery, PagedResult<OrderResponse>>
{
    public async Task<Result<PagedResult<OrderResponse>>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orderRepository.GetAllAsync(
            request.Page, request.PageSize,
            request.Status, request.Search,
            request.FromDate, request.ToDate,
            request.SortBy, request.SortOrder,
            cancellationToken);

        return Result<PagedResult<OrderResponse>>.Success(new PagedResult<OrderResponse>
        {
            Items = items.Adapt<List<OrderResponse>>(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
