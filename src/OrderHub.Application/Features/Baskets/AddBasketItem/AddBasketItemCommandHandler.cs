using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Baskets.AddBasketItem;

public sealed class AddBasketItemCommandHandler(
    IUserContext userContext,
    IBasketRepository basketRepository,
    IProductRepository productRepository)
    : ICommandHandler<AddBasketItemCommand, BasketResponse>
{
    public async Task<Result<BasketResponse>> Handle(AddBasketItemCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Result<BasketResponse>.Failure(BasketErrors.ProductNotFound(request.ProductId));

        if (product.IsDeleted)
            return Result<BasketResponse>.Failure(BasketErrors.ProductUnavailable(request.ProductId));

        var basket = await basketRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? new Basket { UserId = userId };

        var existingItem = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;

            existingItem.Quantity = newQuantity;
        }
        else
        {
            basket.Items.Add(new BasketItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                UnitPrice = product.Price,
                Quantity = request.Quantity
            });
        }

        await basketRepository.SetAsync(basket, cancellationToken);

        return Result<BasketResponse>.Success(basket.Adapt<BasketResponse>());
    }
}
