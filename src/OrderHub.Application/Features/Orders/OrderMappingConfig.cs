using Mapster;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders;

public sealed class OrderMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Items, src => src.Items);

        config.NewConfig<OrderItem, OrderItemResponse>()
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : string.Empty)
            .Map(dest => dest.Subtotal, src => src.UnitPrice * src.Quantity);
    }
}
