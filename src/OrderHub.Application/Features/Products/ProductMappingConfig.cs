using Mapster;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateProduct.UpdateProductCommand, Product>()
            .Ignore(p => p.Id)
            .Ignore(p => p.SKU)
            .Ignore(p => p.CreatedAt)
            .Ignore(p => p.UpdatedAt!)
            .Ignore(p => p.IsActive);
    }
}
