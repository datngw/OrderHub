using OrderHub.Domain.Common;

namespace OrderHub.Domain.Products;

public static class ProductErrors
{
    public static Error NotFound =>
        Error.NotFound("Products.NotFound", "Product was not found.");

    public static Error NotFoundById(Guid id) =>
        Error.NotFound("Products.NotFound", $"Product with ID '{id}' was not found.");

    public static Error SkuAlreadyExists(string sku) =>
        Error.Conflict("Products.SkuAlreadyExists", $"Product with SKU '{sku}' already exists.");
}
