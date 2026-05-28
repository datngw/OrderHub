using OrderHub.Domain.Common;

namespace OrderHub.Domain.Orders;

public static class OrderErrors
{
    public static Error NotFoundById(Guid id) =>
        new("Orders.NotFound", $"Order with ID '{id}' was not found.", ErrorType.NotFound);

    public static Error EmptyOrder =>
        new("Orders.EmptyOrder", "Order must contain at least one item.", ErrorType.Validation);

    public static Error ProductNotFound(Guid productId) =>
        new("Orders.ProductNotFound", $"Product with ID '{productId}' was not found.", ErrorType.NotFound);

    public static Error ProductUnavailable(Guid productId) =>
        new("Orders.ProductUnavailable", $"Product with ID '{productId}' is not available.", ErrorType.Validation);

    public static Error InsufficientStock(string productName, int requested, int available) =>
        new("Orders.InsufficientStock", $"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}.", ErrorType.Validation);

    public static Error AlreadyCancelled =>
        new("Orders.AlreadyCancelled", "Order is already cancelled.", ErrorType.Conflict);

    public static Error CannotBeCancelled =>
        new("Orders.CannotBeCancelled", "Only orders in 'Pending' status can be cancelled.", ErrorType.Validation);

    public static Error InvalidStatusTransition(OrderStatusEnum current, OrderStatusEnum requested) =>
        new("Orders.InvalidStatusTransition", $"Cannot transition order from '{current}' to '{requested}'.", ErrorType.Validation);

    public static Error Forbidden =>
        new("Orders.Forbidden", "You do not have permission to access this order.", ErrorType.Forbidden);
}
