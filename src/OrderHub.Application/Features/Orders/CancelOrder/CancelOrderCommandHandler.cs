using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
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
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<CancelOrderCommandHandler> logger)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);

            if (order is null)
            {
                logger.LogWarning("Order cancellation failed: order {OrderId} not found", request.OrderId);
                return Result.Failure(OrderErrors.NotFoundById(request.OrderId));
            }

            if (!userContext.IsAdmin && order.UserId != userContext.UserId)
            {
                logger.LogWarning("Order cancellation denied: user {UserId} attempted to cancel order {OrderId} belonging to another user",
                    userContext.UserId, request.OrderId);
                return Result.Failure(OrderErrors.Forbidden);
            }

            if (order.Status == OrderStatusEnum.Cancelled)
            {
                logger.LogWarning("Order cancellation failed: order {OrderId} already cancelled", request.OrderId);
                return Result.Failure(OrderErrors.AlreadyCancelled);
            }

            if (order.Status != OrderStatusEnum.Pending)
            {
                logger.LogWarning("Order cancellation failed: order {OrderId} is in status {Status} and cannot be cancelled",
                    request.OrderId, order.Status);
                return Result.Failure(OrderErrors.CannotBeCancelled);
            }

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await productRepository.LockForUpdateAsync(productIds, cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in order.Items)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                    product.Stock += item.Quantity;
            }

            order.Status = OrderStatusEnum.Cancelled;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            cache.InvalidateReports();
            cache.InvalidateProducts();

            logger.LogInformation("Order {OrderId} cancelled by user {UserId}", request.OrderId, userContext.UserId);

            return Result.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
