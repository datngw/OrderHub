using FluentValidation;

namespace OrderHub.Application.Features.Baskets.UpdateBasketItem;

public sealed class UpdateBasketItemCommandValidator : AbstractValidator<UpdateBasketItemCommand>
{
    public UpdateBasketItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99).WithMessage("Quantity must be between 1 and 99.");
    }
}
