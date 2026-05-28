using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IUserContext userContext,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

            if (order is null)
                return Result.Failure(OrderErrors.NotFoundById(request.OrderId));

            if (!userContext.IsAdmin && order.UserId != userContext.UserId)
                return Result.Failure(OrderErrors.Forbidden);

            if (order.Status == OrderStatusEnum.Cancelled)
                return Result.Failure(OrderErrors.AlreadyCancelled);

            if (order.Status != OrderStatusEnum.Pending)
                return Result.Failure(OrderErrors.CannotBeCancelled);

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await productRepository.LockForUpdateAsync(productIds, cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            var trackedOrder = await orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
            if (trackedOrder is null)
                return Result.Failure(OrderErrors.NotFoundById(request.OrderId));

            if (trackedOrder.Status != OrderStatusEnum.Pending)
                return Result.Failure(OrderErrors.CannotBeCancelled);

            foreach (var item in trackedOrder.Items)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                    product.Stock += item.Quantity;
            }

            trackedOrder.Status = OrderStatusEnum.Cancelled;
            trackedOrder.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
