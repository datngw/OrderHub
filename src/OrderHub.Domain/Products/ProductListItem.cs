namespace OrderHub.Domain.Products;

public record ProductListItem(
    Guid Id,
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    bool IsActive,
    DateTime CreatedAt);
