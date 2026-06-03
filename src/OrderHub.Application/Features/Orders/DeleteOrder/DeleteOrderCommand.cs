using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Orders.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId) : ICommand;
