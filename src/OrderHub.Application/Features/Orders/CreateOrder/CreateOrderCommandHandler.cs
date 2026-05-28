using Mapster;
using Microsoft.Extensions.Caching.Memory;
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
    IMemoryCache cache)
    : ICommandHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var userId = userContext.UserId;
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

            var lockedProducts = await productRepository.LockForUpdateAsync(productIds, cancellationToken);
            var productMap = lockedProducts.ToDictionary(p => p.Id);

            var orderItems = new List<OrderItem>();
            var errors = new List<Error>();

            foreach (var item in request.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add(OrderErrors.ProductNotFound(item.ProductId));
                    continue;
                }

                if (!product.IsActive)
                {
                    errors.Add(OrderErrors.ProductUnavailable(item.ProductId));
                    continue;
                }

                if (product.Stock < item.Quantity)
                {
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
                return Result<OrderResponse>.Failure(errors[0]);

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

            return Result<OrderResponse>.Success(order.Adapt<OrderResponse>());
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
