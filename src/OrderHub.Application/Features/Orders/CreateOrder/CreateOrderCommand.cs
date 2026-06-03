using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public record CreateOrderCommand(string? Note) : ICommand<OrderResponse>;
