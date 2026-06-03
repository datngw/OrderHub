using Mapster;
using OrderHub.Domain.Baskets;

namespace OrderHub.Application.Features.Baskets;

public sealed class BasketMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Basket, BasketResponse>()
            .Map(dest => dest.Items, src => src.Items);

        config.NewConfig<BasketItem, BasketItemResponse>()
            .Map(dest => dest.LineTotal, src => src.UnitPrice * src.Quantity);
    }
}
