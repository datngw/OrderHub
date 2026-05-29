using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<UpdateOrderStatusCommandHandler> logger)
    : ICommandHandler<UpdateOrderStatusCommand>
{
    private static readonly Dictionary<OrderStatusEnum, OrderStatusEnum> AllowedTransitions = new()
    {
        [OrderStatusEnum.Pending] = OrderStatusEnum.Confirmed,
        [OrderStatusEnum.Confirmed] = OrderStatusEnum.Shipped,
        [OrderStatusEnum.Shipped] = OrderStatusEnum.Delivered
    };

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order status update failed: order {OrderId} not found", request.OrderId);
            return Result.Failure(OrderErrors.NotFoundById(request.OrderId));
        }

        if (order.Status == OrderStatusEnum.Cancelled)
        {
            logger.LogWarning("Order status update failed: order {OrderId} is already cancelled", request.OrderId);
            return Result.Failure(OrderErrors.AlreadyCancelled);
        }

        if (!AllowedTransitions.TryGetValue(order.Status, out var expectedNext))
        {
            logger.LogWarning("Order status update failed: invalid transition from {CurrentStatus} to {RequestedStatus} for order {OrderId}",
                order.Status, request.NewStatus, request.OrderId);
            return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, request.NewStatus));
        }

        if (request.NewStatus != expectedNext)
        {
            logger.LogWarning("Order status update failed: invalid transition from {CurrentStatus} to {RequestedStatus} for order {OrderId}",
                order.Status, request.NewStatus, request.OrderId);
            return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, request.NewStatus));
        }

        var previousStatus = order.Status;
        order.Status = request.NewStatus;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateReports();

        logger.LogInformation("Order {OrderId} status updated from {PreviousStatus} to {NewStatus}",
            request.OrderId, previousStatus, request.NewStatus);

        return Result.Success();
    }
}
