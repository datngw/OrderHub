using OrderHub.Domain.Common;

namespace OrderHub.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NotFound =
        new("Products.NotFound", "Product was not found.", ErrorType.NotFound);

    public static Error NotFoundById(Guid id) =>
        new("Products.NotFound", $"Product with ID '{id}' was not found.", ErrorType.NotFound);

    public static Error SkuAlreadyExists(string sku) =>
        new("Products.SkuAlreadyExists", $"Product with SKU '{sku}' already exists.", ErrorType.Conflict);
}
