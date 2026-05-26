using Microsoft.EntityFrameworkCore;

using OrderHub.Domain.Products;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public class ProductRepository(OrderHubDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Products.FindAsync([id], ct);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct)
    {
        return await context.Products.FirstOrDefaultAsync(p => p.SKU == sku, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct)
    {
        return await context.Products.AnyAsync(p => p.SKU == sku, ct);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetFilteredAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? search,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        query = ApplySorting(query, sortBy, sortOrder);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public void Add(Product product) => context.Products.Add(product);

    public void Update(Product product) => context.Products.Update(product);

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, string? sortOrder)
    {
        var isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => isDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "category" => isDesc ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "sku" => isDesc ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
            _ => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };
    }
}
