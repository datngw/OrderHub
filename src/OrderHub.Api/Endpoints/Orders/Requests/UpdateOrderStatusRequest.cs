using OrderHub.Domain.Orders;

namespace OrderHub.Api.Endpoints.Orders.Requests;

public record UpdateOrderStatusRequest(OrderStatusEnum Status);
