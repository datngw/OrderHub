using Mapster;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.CreateProduct;

public sealed class CreateProductCommandHandler(DbContext dbContext)
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.Set<Product>().AnyAsync(p => p.SKU == request.SKU, cancellationToken))
            return Result<ProductResponse>.Failure(ProductErrors.SkuAlreadyExists(request.SKU));

        var product = request.Adapt<Product>();
        dbContext.Set<Product>().Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Adapt<ProductResponse>();
    }
}
