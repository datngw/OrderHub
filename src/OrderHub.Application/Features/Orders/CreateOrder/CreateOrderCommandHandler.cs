using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IUserContext userContext,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    ILogger<CreateOrderCommandHandler> logger)
    : ICommandHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var userId = userContext.UserId;
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

            logger.LogInformation("Creating order for user {UserId} with {ItemCount} items", userId, productIds.Count);

            var lockedProducts = await productRepository.LockForUpdateAsync(productIds, cancellationToken);
            var productMap = lockedProducts.ToDictionary(p => p.Id);

            var orderItems = new List<OrderItem>();
            var errors = new List<Error>();

            foreach (var item in request.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    logger.LogWarning("Order creation: product {ProductId} not found for user {UserId}", item.ProductId, userId);
                    errors.Add(OrderErrors.ProductNotFound(item.ProductId));
                    continue;
                }

                if (!product.IsActive)
                {
                    logger.LogWarning("Order creation: product {ProductId} unavailable for user {UserId}", item.ProductId, userId);
                    errors.Add(OrderErrors.ProductUnavailable(item.ProductId));
                    continue;
                }

                if (product.Stock < item.Quantity)
                {
                    logger.LogWarning("Order creation: insufficient stock for product {ProductId} (requested {RequestedQty}, available {AvailableQty}) for user {UserId}",
                        item.ProductId, item.Quantity, product.Stock, userId);
                    errors.Add(OrderErrors.InsufficientStock(product.Name, item.Quantity, product.Stock));
                    continue;
                }

                product.Stock -= item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            if (errors.Count > 0)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                logger.LogWarning("Order creation failed for user {UserId}: {ErrorCode}", userId, errors[0].Code);
                return Result<OrderResponse>.Failure(errors[0]);
            }

            var totalAmount = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatusEnum.Pending,
                TotalAmount = totalAmount,
                Items = orderItems
            };

            orderRepository.Add(order);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            cache.InvalidateReports();
            cache.InvalidateProducts();

            logger.LogInformation("Order {OrderId} created for user {UserId} with total {TotalAmount}", order.Id, userId, totalAmount);

            return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
