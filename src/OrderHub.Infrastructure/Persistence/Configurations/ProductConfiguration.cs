using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Products;

namespace OrderHub.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(ProductConstraints.SkuMaxLength);

        builder.HasIndex(p => p.SKU).IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(ProductConstraints.NameMaxLength);

        builder.Property(p => p.Description)
            .HasMaxLength(ProductConstraints.DescriptionMaxLength);

        builder.Property(p => p.Price)
            .HasColumnType($"decimal({ProductConstraints.PricePrecision},{ProductConstraints.PriceScale})");

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(ProductConstraints.CategoryMaxLength);

        // Covering index for default query: WHERE IsActive ORDER BY CreatedAt DESC
        builder.HasIndex(p => new { p.IsActive, p.CreatedAt })
            .IsDescending(false, true)
            .IncludeProperties("Id", "SKU", "Name", "Description", "Price", "Stock", "Category")
            .HasDatabaseName("IX_Products_IsActive_CreatedAt");

        // Covering index for category filter: WHERE IsActive AND Category = ? ORDER BY CreatedAt DESC
        builder.HasIndex(p => new { p.IsActive, p.Category, p.CreatedAt })
            .IsDescending(false, false, true)
            .IncludeProperties("Id", "SKU", "Name", "Description", "Price", "Stock")
            .HasDatabaseName("IX_Products_IsActive_Category_CreatedAt");

        // Covering index for price range: WHERE IsActive AND Price BETWEEN ? AND ?
        builder.HasIndex(p => new { p.IsActive, p.Price })
            .IncludeProperties("Id", "SKU", "Name", "Description", "Stock", "Category", "CreatedAt")
            .HasDatabaseName("IX_Products_IsActive_Price");
    }
}
