using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(DbContext dbContext)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Set<Product>().FindAsync([request.Id], cancellationToken);

        if (product is null)
            return Result.Failure(Error.NotFound(nameof(Product), request.Id));

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
