using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid OrderId, Guid UserId, bool IsAdmin)
    : ICommand
{
    public CancelOrderCommand(Guid orderId) : this(orderId, Guid.Empty, false) { }
}
