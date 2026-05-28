namespace OrderHub.Application.Features.Orders;

public record OrderResponse(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
