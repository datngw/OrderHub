using Mapster;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(DbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Set<Product>()
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.IsActive)
            .ProjectToType<ProductResponse>()
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));

        return product;
    }
}
