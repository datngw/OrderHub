using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Domain.Orders;
using OrderHub.Domain.Products;
using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence;

public class OrderHubDbContext : DbContext
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public OrderHubDbContext(DbContextOptions<OrderHubDbContext> options, IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderHubDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _dateTimeProvider.UtcNow.UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
