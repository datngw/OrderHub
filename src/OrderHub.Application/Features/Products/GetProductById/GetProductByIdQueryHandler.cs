using Mapster;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Products;
using OrderHub.Domain.Common;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null || !product.IsActive)
            return Result<ProductResponse>.Failure(ProductErrors.NotFoundById(request.Id));

        return Result<ProductResponse>.Success(product.Adapt<ProductResponse>());
    }
}
