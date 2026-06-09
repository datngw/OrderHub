using OrderHub.Domain.Common;

namespace OrderHub.Domain.Products;

public class Product : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public List<string> GalleryImageUrls { get; set; } = [];
}
