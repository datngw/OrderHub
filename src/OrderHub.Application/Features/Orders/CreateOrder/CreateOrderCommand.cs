using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public record CreateOrderItem(Guid ProductId, int Quantity);

public record CreateOrderCommand(List<CreateOrderItem> Items)
    : ICommand<OrderResponse>;
