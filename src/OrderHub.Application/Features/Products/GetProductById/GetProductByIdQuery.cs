using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Products;

namespace OrderHub.Application.Features.Products.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
