namespace OrderHub.Api.Endpoints.Orders.Requests;

public record CreateOrderRequest(
    string? Note,
    string Email,
    string FullName,
    string Phone,
    string Province,
    string District,
    string Ward,
    string StreetAddress);
