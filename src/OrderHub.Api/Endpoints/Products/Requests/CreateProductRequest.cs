namespace OrderHub.Api.Endpoints.Products.Requests;

public record CreateProductRequest(
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string? MainImageUrl,
    List<string>? GalleryImageUrls,
    bool IsActive = true);
