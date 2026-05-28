using Microsoft.Extensions.Caching.Memory;
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
    IMemoryCache cache)
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
            return Result.Failure(OrderErrors.NotFoundById(request.OrderId));

        if (order.Status == OrderStatusEnum.Cancelled)
            return Result.Failure(OrderErrors.AlreadyCancelled);

        if (!AllowedTransitions.TryGetValue(order.Status, out var expectedNext))
            return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, request.NewStatus));

        if (request.NewStatus != expectedNext)
            return Result.Failure(OrderErrors.InvalidStatusTransition(order.Status, request.NewStatus));

        order.Status = request.NewStatus;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateReports();

        return Result.Success();
    }
}
