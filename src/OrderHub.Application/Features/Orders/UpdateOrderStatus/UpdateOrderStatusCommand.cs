using OrderHub.Application.Common.Messaging;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.UpdateOrderStatus;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatusEnum NewStatus)
    : ICommand;
