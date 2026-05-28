using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId, Guid UserId, bool IsAdmin)
    : IQuery<OrderResponse>
{
    public GetOrderByIdQuery(Guid orderId) : this(orderId, Guid.Empty, false) { }
}
