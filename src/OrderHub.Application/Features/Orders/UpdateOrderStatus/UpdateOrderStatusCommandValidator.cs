using FluentValidation;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly HashSet<OrderStatusEnum> ValidTransitions =
    [
        OrderStatusEnum.Confirmed,
        OrderStatusEnum.Shipped,
        OrderStatusEnum.Delivered
    ];

    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.NewStatus)
            .Must(s => ValidTransitions.Contains(s))
            .WithMessage("Invalid status. Allowed values: Confirmed, Shipped, Delivered.");
    }
}
