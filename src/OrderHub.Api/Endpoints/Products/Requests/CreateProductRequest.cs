namespace OrderHub.Api.Features.Products.Requests;

public record CreateProductRequest(
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category);
