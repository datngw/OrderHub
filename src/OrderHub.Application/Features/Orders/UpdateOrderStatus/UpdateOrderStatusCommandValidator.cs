using FluentValidation;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly HashSet<string> ValidStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            OrderStatusEnum.Confirmed.ToString(),
            OrderStatusEnum.Shipped.ToString(),
            OrderStatusEnum.Delivered.ToString()
        };

    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidStatus)
            .WithMessage("Invalid status. Allowed values: Confirmed, Shipped, Delivered.");
    }

    private static bool BeValidStatus(string status) => ValidStatuses.Contains(status);
}
