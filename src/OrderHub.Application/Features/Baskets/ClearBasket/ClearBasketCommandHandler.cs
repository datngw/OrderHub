using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Baskets;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Baskets.ClearBasket;

public sealed class ClearBasketCommandHandler(
    IUserContext userContext,
    IBasketRepository basketRepository,
    ILogger<ClearBasketCommandHandler> logger)
    : ICommandHandler<ClearBasketCommand>
{
    public async Task<Result> Handle(ClearBasketCommand request, CancellationToken cancellationToken)
    {
        await basketRepository.RemoveAsync(userContext.UserId, cancellationToken);

        logger.LogInformation("Basket cleared for user {UserId}", userContext.UserId);

        return Result.Success();
    }
}
