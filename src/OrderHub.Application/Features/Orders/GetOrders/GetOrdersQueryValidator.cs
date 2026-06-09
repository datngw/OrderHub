using FluentValidation;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.Orders.GetOrders;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    private static readonly HashSet<string> ValidSortFields =
        new(StringComparer.OrdinalIgnoreCase) { "createdat", "totalamount", "status" };

    private static readonly HashSet<string> ValidSortOrders =
        new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

    private static readonly HashSet<string> ValidStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            OrderStatusEnum.Pending.ToString(),
            OrderStatusEnum.Confirmed.ToString(),
            OrderStatusEnum.Shipped.ToString(),
            OrderStatusEnum.Delivered.ToString(),
            OrderStatusEnum.Cancelled.ToString()
        };

    public GetOrdersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status!)
                .Must(BeValidStatus)
                .WithMessage("Invalid status. Allowed: Pending, Confirmed, Shipped, Delivered, Cancelled");
        });

        When(x => x.Search is not null, () =>
        {
            RuleFor(x => x.Search!)
                .MaximumLength(200);
        });

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate!.Value)
                .LessThanOrEqualTo(x => x.ToDate!.Value)
                .WithMessage("FromDate must be earlier than or equal to ToDate");
        });

        When(x => x.SortBy is not null, () =>
        {
            RuleFor(x => x.SortBy!)
                .Must(BeValidSortField)
                .WithMessage("Invalid sort field. Allowed: createdAt, totalAmount, status");
        });

        When(x => x.SortOrder is not null, () =>
        {
            RuleFor(x => x.SortOrder!)
                .Must(BeValidSortOrder)
                .WithMessage("Sort order must be 'asc' or 'desc'");
        });
    }

    private static bool BeValidStatus(string status) => ValidStatuses.Contains(status);
    private static bool BeValidSortField(string field) => ValidSortFields.Contains(field);
    private static bool BeValidSortOrder(string order) => ValidSortOrders.Contains(order);
}
