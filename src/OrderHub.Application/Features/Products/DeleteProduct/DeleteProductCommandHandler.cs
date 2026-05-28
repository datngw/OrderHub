using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IMemoryCache cache)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result.Failure(ProductErrors.NotFoundById(request.Id));

        if (!product.IsActive)
            return Result.Success();

        product.IsActive = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cache.InvalidateProducts(request.Id);

        return Result.Success();
    }
}
