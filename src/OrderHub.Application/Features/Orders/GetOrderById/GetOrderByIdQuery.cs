using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderResponse>;
