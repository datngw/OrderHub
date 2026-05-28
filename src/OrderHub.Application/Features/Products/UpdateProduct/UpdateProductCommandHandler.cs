using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));

        request.Adapt(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Adapt<ProductResponse>();
    }
}
