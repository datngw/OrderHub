using FluentValidation;

namespace OrderHub.Application.Features.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.").MaximumLength(50).WithMessage("SKU must not exceed 50 characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
        RuleFor(x => x.Description).MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Category is required.").MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
    }
}
