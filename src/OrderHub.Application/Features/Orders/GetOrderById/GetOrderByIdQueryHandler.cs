using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    IUserContext userContext,
    IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Result<OrderResponse>.Failure(OrderErrors.NotFoundById(request.OrderId));

        if (!userContext.IsAdmin && order.UserId != userContext.UserId)
            return Result<OrderResponse>.Failure(OrderErrors.Forbidden);

        return order.Adapt<OrderResponse>();
    }
}
