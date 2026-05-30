using OrderHub.Domain.Common;

namespace OrderHub.Domain.Orders;

public static class OrderErrors
{
    public static Error NotFoundById(Guid id) =>
        Error.NotFound("Orders.NotFound", $"Order with ID '{id}' was not found.");

    public static Error EmptyOrder =>
        Error.Problem("Orders.EmptyOrder", "Order must contain at least one item.");

    public static Error ProductNotFound(Guid productId) =>
        Error.NotFound("Orders.ProductNotFound", $"Product with ID '{productId}' was not found.");

    public static Error ProductUnavailable(Guid productId) =>
        Error.Problem("Orders.ProductUnavailable", $"Product with ID '{productId}' is not available.");

    public static Error InsufficientStock(string productName, int requested, int available) =>
        Error.Problem("Orders.InsufficientStock", $"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}.");

    public static Error AlreadyCancelled =>
        Error.Conflict("Orders.AlreadyCancelled", "Order is already cancelled.");

    public static Error CannotBeCancelled =>
        Error.Problem("Orders.CannotBeCancelled", "Only orders in 'Pending' status can be cancelled.");

    public static Error InvalidStatusTransition(OrderStatusEnum current, OrderStatusEnum requested) =>
        Error.Problem("Orders.InvalidStatusTransition", $"Cannot transition order from '{current}' to '{requested}'.");

    public static Error Forbidden =>
        Error.Problem("Orders.Forbidden", "You do not have permission to access this order.");
}
