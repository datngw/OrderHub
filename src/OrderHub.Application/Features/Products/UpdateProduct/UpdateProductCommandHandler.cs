using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Common.Persistence;

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

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.Category = request.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Adapt<ProductResponse>();
    }
}
