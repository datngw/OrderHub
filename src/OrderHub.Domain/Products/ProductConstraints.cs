namespace OrderHub.Domain.Products;

public static class ProductConstraints
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int CategoryMaxLength = 100;
    public const decimal PriceMinValue = 0;
    public const int StockMinValue = 0;
    public const int ImageUrlMaxLength = 500;
    public const string DefaultImageUrl = "https://placehold.co/600x400/png?text=No+Image";
    public const int MaxGalleryImages = 10;
    public const int PricePrecision = 18;
    public const int PriceScale = 2;
}
