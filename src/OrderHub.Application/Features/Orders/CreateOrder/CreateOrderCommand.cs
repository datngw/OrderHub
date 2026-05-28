using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public record CreateOrderItem(Guid ProductId, int Quantity);

public record CreateOrderCommand(Guid UserId, List<CreateOrderItem> Items)
    : ICommand<OrderResponse>
{
    public CreateOrderCommand(List<CreateOrderItem> items) : this(Guid.Empty, items) { }
}
