using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.DeleteOrder;

public sealed class DeleteOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<DeleteOrderCommandHandler> logger)
    : ICommandHandler<DeleteOrderCommand>
{
    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure(OrderErrors.NotFoundById(request.OrderId));

        if (order.IsDeleted)
            return Result.Success();

        order.IsDeleted = true;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateReports();

        logger.LogInformation("Order soft-deleted: {OrderId}", request.OrderId);

        return Result.Success();
    }
}
