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
        var userId = userContext.UserId;
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        logger.LogInformation("Creating order for user {UserId} with {ItemCount} items", userId, productIds.Count);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var lockedProducts = await productRepository.LockForUpdateAsync(productIds, ct);
            var productMap = lockedProducts.ToDictionary(p => p.Id);

            // Phase 1: Validate all items — no mutations
            var errors = new List<Error>();
            var validatedItems = new List<(Product Product, int Quantity)>();

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

                validatedItems.Add((product, item.Quantity));
            }

            if (errors.Count > 0)
            {
                logger.LogWarning("Order creation failed for user {UserId}: {ErrorCode}", userId, errors[0].Code);
                return Result<OrderResponse>.Failure(errors[0]);
            }

            // Phase 2: Mutate — only runs when all items are valid
            var orderItems = new List<OrderItem>();
            foreach (var (product, quantity) in validatedItems)
            {
                product.Stock -= quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
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

            cache.InvalidateReports();
            cache.InvalidateProducts();

            logger.LogInformation("Order {OrderId} created for user {UserId} with total {TotalAmount}", order.Id, userId, totalAmount);

            return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
        }, cancellationToken);
    }
}
