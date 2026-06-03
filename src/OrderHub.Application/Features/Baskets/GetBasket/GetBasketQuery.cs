using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Baskets;

namespace OrderHub.Application.Features.Baskets.GetBasket;

public record GetBasketQuery : IQuery<BasketResponse>;
