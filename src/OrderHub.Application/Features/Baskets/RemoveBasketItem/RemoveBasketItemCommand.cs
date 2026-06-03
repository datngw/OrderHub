using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Baskets;

namespace OrderHub.Application.Features.Baskets.RemoveBasketItem;

public record RemoveBasketItemCommand(Guid ProductId)
    : ICommand<BasketResponse>;
