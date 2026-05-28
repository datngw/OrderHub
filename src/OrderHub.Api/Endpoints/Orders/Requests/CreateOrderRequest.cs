namespace OrderHub.Api.Endpoints.Orders.Requests;

public record CreateOrderRequest(List<OrderItemRequest> Items);

public record OrderItemRequest(Guid ProductId, int Quantity);
