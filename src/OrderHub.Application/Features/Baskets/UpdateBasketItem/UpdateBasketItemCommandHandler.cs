using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Baskets.UpdateBasketItem;

public sealed class UpdateBasketItemCommandHandler(
    IUserContext userContext,
    IBasketRepository basketRepository,
    IProductRepository productRepository)
    : ICommandHandler<UpdateBasketItemCommand, BasketResponse>
{
    public async Task<Result<BasketResponse>> Handle(UpdateBasketItemCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        var basket = await basketRepository.GetByUserIdAsync(userId, cancellationToken);

        if (basket is null)
            return Result<BasketResponse>.Failure(BasketErrors.NotFound);

        var item = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is null)
            return Result<BasketResponse>.Failure(BasketErrors.ItemNotFound(request.ProductId));

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Result<BasketResponse>.Failure(BasketErrors.ProductNotFound(request.ProductId));

        if (product.Stock < request.Quantity)
            return Result<BasketResponse>.Failure(BasketErrors.InsufficientStock(request.Quantity, product.Stock));

        item.Quantity = request.Quantity;
        item.UnitPrice = product.Price;
        item.ProductName = product.Name;

        await basketRepository.SetAsync(basket, cancellationToken);

        return Result<BasketResponse>.Success(basket.Adapt<BasketResponse>());
    }
}
