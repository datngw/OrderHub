using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Orders.UpdateOrderStatus;

public record UpdateOrderStatusCommand(Guid OrderId, string NewStatus)
    : ICommand;
