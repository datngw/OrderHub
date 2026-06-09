using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Exceptions;
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

        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        var errors = new List<Error>();
        var itemSnapshots = new List<(BasketItem Item, Product Product)>();

        foreach (var item in basket.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                errors.Add(OrderErrors.ProductNotFound(item.ProductId));
                continue;
            }

            if (product.IsDeleted || !product.IsActive)
            {
                errors.Add(OrderErrors.ProductUnavailable(item.ProductId));
                continue;
            }

            if (product.Stock < item.Quantity)
            {
                errors.Add(OrderErrors.InsufficientStock(product.Name, item.Quantity, product.Stock));
                continue;
            }

            itemSnapshots.Add((item, product));
        }

        if (errors.Count > 0)
            return Result<OrderResponse>.Failure(errors[0]);

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var orderItems = new List<OrderItem>();

                foreach (var (item, product) in itemSnapshots)
                {
                    var affected = await productRepository.TryDecrementStockAsync(item.ProductId, item.Quantity, ct);

                    if (affected == 0)
                    {
                        logger.LogWarning(
                            "Atomic decrement failed for product {ProductId} (requested {Quantity}). " +
                            "Stock may have been exhausted by a concurrent order.",
                            item.ProductId, item.Quantity);

                        throw new AppException(OrderErrors.InsufficientStock(product.Name, item.Quantity, available: 0));
                    }

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                    });
                }

                var totalAmount = orderItems.Sum(i => i.UnitPrice * i.Quantity);

                var order = new Order
                {
                    UserId = userId,
                    Status = OrderStatusEnum.Pending,
                    TotalAmount = totalAmount,
                    Items = orderItems,
                    Note = request.Note ?? string.Empty,
                    Email = request.Email,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Province = request.Province,
                    District = request.District,
                    Ward = request.Ward,
                    StreetAddress = request.StreetAddress,
                };

                orderRepository.Add(order);

                await basketRepository.RemoveAsync(userId, ct);

                // Invalidate product and report caches
                cache.InvalidateProducts();
                cache.InvalidateReports();

                logger.LogInformation(
                    "Order {OrderId} created for user {UserId} with total {TotalAmount}",
                    order.Id, userId, totalAmount);

                return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
            }, cancellationToken);
        }
        catch (AppException ex)
        {
            return Result<OrderResponse>.Failure(ex.Error);
        }
    }
}
