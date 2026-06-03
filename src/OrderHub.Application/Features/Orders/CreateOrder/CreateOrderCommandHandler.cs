using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Orders;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IUserContext userContext,
    IBasketRepository basketRepository,
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
        var basket = await basketRepository.GetByUserIdAsync(userId, cancellationToken);

        if (basket is null || basket.Items.Count == 0)
            return Result<OrderResponse>.Failure(OrderErrors.EmptyOrder);

        var productIds = basket.Items.Select(i => i.ProductId).Distinct().ToList();

        logger.LogInformation("Creating order from basket for user {UserId} with {ItemCount} product(s)", userId, productIds.Count);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var lockedProducts = await productRepository.LockForUpdateAsync(productIds, ct);
            var productMap = lockedProducts.ToDictionary(p => p.Id);

            // Phase 1: Validate all items — no mutations
            var errors = new List<Error>();
            var validatedItems = new List<(Product Product, int Quantity)>();

            foreach (var item in basket.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add(OrderErrors.ProductNotFound(item.ProductId));
                    continue;
                }

                if (product.IsDeleted)
                {
                    errors.Add(OrderErrors.ProductUnavailable(item.ProductId));
                    continue;
                }

                if (product.Stock < item.Quantity)
                {
                    errors.Add(OrderErrors.InsufficientStock(product.Name, item.Quantity, product.Stock));
                    continue;
                }

                validatedItems.Add((product, item.Quantity));
            }

            if (errors.Count > 0)
                return Result<OrderResponse>.Failure(errors[0]);

            // Phase 2: Mutate — deduct stock, create order items with price snapshot
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
                Items = orderItems,
                Note = request.Note ?? string.Empty
            };

            orderRepository.Add(order);

            // Clear basket after successful checkout
            await basketRepository.RemoveAsync(userId, ct);

            cache.InvalidateReports();

            logger.LogInformation("Order {OrderId} created for user {UserId} with total {TotalAmount}", order.Id, userId, totalAmount);

            return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
        }, cancellationToken);
    }
}
