using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await productRepository.ExistsBySkuAsync(request.SKU, cancellationToken))
            return Result<ProductResponse>.Failure(ProductErrors.SkuAlreadyExists(request.SKU));

        var product = request.Adapt<Product>();
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Adapt<ProductResponse>();
    }
}
