using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Baskets;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Baskets.GetBasket;

public sealed class GetBasketQueryHandler(
    IUserContext userContext,
    IBasketRepository basketRepository)
    : IQueryHandler<GetBasketQuery, BasketResponse>
{
    public async Task<Result<BasketResponse>> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var basket = await basketRepository.GetByUserIdAsync(userContext.UserId, cancellationToken);

        if (basket is null)
            return Result<BasketResponse>.Success(new BasketResponse(userContext.UserId, 0, 0, []));

        return Result<BasketResponse>.Success(basket.Adapt<BasketResponse>());
    }
}
