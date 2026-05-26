using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Products;

namespace OrderHub.Application.Features.Products.CreateProduct;

public record CreateProductCommand(string SKU, string Name, string Description, decimal Price, int Stock, string Category)
    : ICommand<ProductResponse>;
