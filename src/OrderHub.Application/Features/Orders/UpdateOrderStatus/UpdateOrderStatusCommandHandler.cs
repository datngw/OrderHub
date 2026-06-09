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
        if (!Enum.TryParse<OrderStatusEnum>(request.NewStatus, ignoreCase: true, out var newStatus))
            return Result.Failure(OrderErrors.InvalidStatusTransition(default, newStatus));

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orderRepository.GetByIdForUpdateAsync(request.OrderId, ct);

            if (order is null)
                return Result.Failure(OrderErrors.NotFoundById(request.OrderId));

            if (order.Status == OrderStatusEnum.Cancelled)
                return Result.Failure(OrderErrors.AlreadyCancelled);

            if (!AllowedTransitions.TryGetValue(order.Status, out var expectedNext))
                return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, newStatus));

            if (newStatus != expectedNext)
                return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, newStatus));

            var previousStatus = order.Status;
            order.Status = newStatus;

            cache.InvalidateReports();

            logger.LogInformation("Order {OrderId} status updated from {PreviousStatus} to {NewStatus}",
                request.OrderId, previousStatus, newStatus);

            return Result.Success();
        }, cancellationToken);
    }
}
