namespace OrderHub.Application.Features.Orders;

public record OrderResponse(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Email,
    string FullName,
    string Phone,
    string Province,
    string District,
    string Ward,
    string StreetAddress,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string Note);
