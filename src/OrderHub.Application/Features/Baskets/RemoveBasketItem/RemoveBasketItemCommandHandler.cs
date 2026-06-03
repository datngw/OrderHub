using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Baskets.RemoveBasketItem;

public sealed class RemoveBasketItemCommandHandler(
    IUserContext userContext,
    IBasketRepository basketRepository)
    : ICommandHandler<RemoveBasketItemCommand, BasketResponse>
{
    public async Task<Result<BasketResponse>> Handle(RemoveBasketItemCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        var basket = await basketRepository.GetByUserIdAsync(userId, cancellationToken);

        if (basket is null)
            return Result<BasketResponse>.Failure(BasketErrors.NotFound);

        var item = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is null)
            return Result<BasketResponse>.Failure(BasketErrors.ItemNotFound(request.ProductId));

        basket.Items.Remove(item);

        if (basket.Items.Count == 0)
        {
            await basketRepository.RemoveAsync(userId, cancellationToken);

            return Result<BasketResponse>.Success(new BasketResponse(userId, 0, 0, []));
        }

        await basketRepository.SetAsync(basket, cancellationToken);

        return Result<BasketResponse>.Success(basket.Adapt<BasketResponse>());
    }
}
