using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Common.Persistence;

using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result.Failure(ProductErrors.NotFoundById(request.Id));

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
