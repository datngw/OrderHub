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
        RuleFor(x => x.MainImageUrl)
            .MaximumLength(ProductConstraints.ImageUrlMaxLength).WithMessage($"Main image URL must not exceed {ProductConstraints.ImageUrlMaxLength} characters.")
            .Must(uri => uri is null || Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Main image URL must be a valid URL.");
        RuleFor(x => x.GalleryImageUrls).Must(urls => urls is null || urls.Count <= ProductConstraints.MaxGalleryImages).WithMessage($"Maximum {ProductConstraints.MaxGalleryImages} gallery images per product.");
        RuleForEach(x => x.GalleryImageUrls).ChildRules(url =>
        {
            url.RuleFor(u => u).NotEmpty().WithMessage("Gallery image URL is required.")
                .MaximumLength(ProductConstraints.ImageUrlMaxLength).WithMessage($"Gallery image URL must not exceed {ProductConstraints.ImageUrlMaxLength} characters.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Gallery image URL must be a valid URL.");
        });
    }
}
