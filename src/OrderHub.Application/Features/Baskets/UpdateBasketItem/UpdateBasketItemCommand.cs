using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Baskets.UpdateBasketItem;

public record UpdateBasketItemCommand(Guid ProductId, int Quantity)
    : ICommand<BasketResponse>;
