namespace OrderHub.Application.Features.Products;

public record ProductResponse(
    Guid Id,
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string? MainImageUrl,
    List<string> GalleryImageUrls,
    bool IsActive,
    DateTime CreatedAt);
