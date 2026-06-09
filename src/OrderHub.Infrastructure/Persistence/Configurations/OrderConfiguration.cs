using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount)
            .HasColumnType($"decimal({ProductConstraints.PricePrecision},{ProductConstraints.PriceScale})");

        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.Email).HasMaxLength(256);
        builder.Property(o => o.FullName).HasMaxLength(200);
        builder.Property(o => o.Phone).HasMaxLength(20);
        builder.Property(o => o.Province).HasMaxLength(200);
        builder.Property(o => o.District).HasMaxLength(200);
        builder.Property(o => o.Ward).HasMaxLength(200);
        builder.Property(o => o.StreetAddress).HasMaxLength(500);

        // ── Indexes ──

        // Composite index for user's orders: WHERE UserId = ... ORDER BY CreatedAt
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });

        // Composite partial index for admin queries: WHERE Status = ... AND CreatedAt range
        // Only indexes non-deleted orders (matches global query filter)
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasFilter("\"IsDeleted\" = false");

        // Standalone CreatedAt for reporting (date-range queries without status filter)
        builder.HasIndex(o => o.CreatedAt);
    }
}
