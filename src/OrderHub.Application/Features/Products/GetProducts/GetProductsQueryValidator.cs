using FluentValidation;

namespace OrderHub.Application.Features.Products.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    private static readonly HashSet<string> ValidSortFields =
        new(StringComparer.OrdinalIgnoreCase) { "createdat", "name", "price", "category", "sku" };

    private static readonly HashSet<string> ValidSortOrders =
        new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        When(x => x.SortBy is not null, () =>
        {
            RuleFor(x => x.SortBy!)
                .Must(BeValidSortField)
                .WithMessage("Invalid sort field. Allowed: createdAt, name, price, category, sku");
        });

        When(x => x.SortOrder is not null, () =>
        {
            RuleFor(x => x.SortOrder!)
                .Must(BeValidSortOrder)
                .WithMessage("Sort order must be 'asc' or 'desc'");
        });
    }

    private static bool BeValidSortField(string field) => ValidSortFields.Contains(field);
    private static bool BeValidSortOrder(string order) => ValidSortOrders.Contains(order);
}
