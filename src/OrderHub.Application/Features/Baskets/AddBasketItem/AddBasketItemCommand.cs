using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Baskets;

namespace OrderHub.Application.Features.Baskets.AddBasketItem;

public record AddBasketItemCommand(Guid ProductId, int Quantity)
    : ICommand<BasketResponse>;
