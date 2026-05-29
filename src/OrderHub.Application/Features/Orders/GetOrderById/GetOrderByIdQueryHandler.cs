using Mapster;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    IUserContext userContext,
    IOrderRepository orderRepository,
    ILogger<GetOrderByIdQueryHandler> logger)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogInformation("Order lookup: order {OrderId} not found", request.OrderId);
            return Result<OrderResponse>.Failure(OrderErrors.NotFoundById(request.OrderId));
        }

        if (!userContext.IsAdmin && order.UserId != userContext.UserId)
        {
            logger.LogWarning("Order access denied: user {UserId} attempted to access order {OrderId}",
                userContext.UserId, request.OrderId);
            return Result<OrderResponse>.Failure(OrderErrors.Forbidden);
        }

        return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
    }
}
