using FluentValidation;
using OrderHub.Domain.Products;

namespace OrderHub.Application.Features.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.").MaximumLength(ProductConstraints.SkuMaxLength).WithMessage($"SKU must not exceed {ProductConstraints.SkuMaxLength} characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(ProductConstraints.NameMaxLength).WithMessage($"Name must not exceed {ProductConstraints.NameMaxLength} characters.");
        RuleFor(x => x.Description).MaximumLength(ProductConstraints.DescriptionMaxLength).WithMessage($"Description must not exceed {ProductConstraints.DescriptionMaxLength} characters.");
        RuleFor(x => x.Price).GreaterThan(ProductConstraints.PriceMinValue).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(ProductConstraints.StockMinValue).WithMessage("Stock cannot be negative.");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Category is required.").MaximumLength(ProductConstraints.CategoryMaxLength).WithMessage($"Category must not exceed {ProductConstraints.CategoryMaxLength} characters.");
    }
}
