using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Orders;

namespace OrderHub.Application.Features.Orders.CreateOrder;

public record CreateOrderCommand(
    string? Note,
    string Email,
    string FullName,
    string Phone,
    string Province,
    string District,
    string Ward,
    string StreetAddress) : ICommand<OrderResponse>;
