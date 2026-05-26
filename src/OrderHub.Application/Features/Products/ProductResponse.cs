namespace OrderHub.Application.Features.Products;

public record ProductResponse(
    Guid Id,
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    bool IsActive,
    DateTime CreatedAt);
