using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Products;

namespace OrderHub.Application.Features.Products.UpdateProduct;

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, int Stock, string Category, string? MainImageUrl, List<string>? GalleryImageUrls, bool IsActive)
    : ICommand<ProductResponse>;
