using FluentValidation;

namespace OrderHub.Application.Features.AdminReports.GetTopProducts;

public sealed class GetTopProductsQueryValidator : AbstractValidator<GetTopProductsQuery>
{
    public GetTopProductsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 100)
            .WithMessage("Top must be between 1 and 100.");
    }
}
