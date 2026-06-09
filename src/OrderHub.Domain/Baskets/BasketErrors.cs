using OrderHub.Domain.Common;

namespace OrderHub.Domain.Baskets;

public static class BasketErrors
{
    public static Error NotFound =>
        Error.NotFound("Baskets.NotFound", "Basket not found.");

    public static Error EmptyBasket =>
        Error.Problem("Baskets.EmptyBasket", "Basket is empty.");

    public static Error ItemNotFound(Guid productId) =>
        Error.NotFound("Baskets.ItemNotFound", $"Item with product ID '{productId}' not found in basket.");

    public static Error ProductNotFound(Guid productId) =>
        Error.NotFound("Baskets.ProductNotFound", $"Product with ID '{productId}' was not found.");

    public static Error ProductUnavailable(Guid productId) =>
        Error.Problem("Baskets.ProductUnavailable", $"Product with ID '{productId}' is not available.");

    public static Error QuantityExceedsLimit =>
        Error.Problem("Baskets.QuantityExceedsLimit", "Tổng số lượng mỗi sản phẩm không được vượt quá 99.");

    public static Error InsufficientStock(int requested, int available) =>
        Error.Problem("Baskets.InsufficientStock", $"Insufficient stock. Requested: {requested}, Available: {available}.");
}
