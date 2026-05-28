using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid OrderId) : ICommand;
