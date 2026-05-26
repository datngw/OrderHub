using OrderHub.Application.Common.Results;

namespace OrderHub.Application.Features.Products;

public static class ProductErrors
{
    public static readonly Error NotFound = new Error(
        "Products.NotFound", "Product was not found.", 404);

    public static Error NotFoundById(Guid id) => new Error(
        "Products.NotFound", $"Product with ID '{id}' was not found.", 404);

    public static Error SkuAlreadyExists(string sku) => new Error(
        "Products.SkuAlreadyExists", $"Product with SKU '{sku}' already exists.", 409);
}
