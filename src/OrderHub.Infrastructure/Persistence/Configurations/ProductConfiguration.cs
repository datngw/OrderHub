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

        builder.HasIndex(p => new { p.Name, p.Category, p.Price });
    }
}
