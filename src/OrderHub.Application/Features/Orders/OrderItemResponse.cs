namespace OrderHub.Application.Features.Orders;

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
