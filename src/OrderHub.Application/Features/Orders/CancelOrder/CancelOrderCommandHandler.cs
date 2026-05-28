using Microsoft.Extensions.Caching.Memory;
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
    IMemoryCache cache)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);

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

            return Result.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
