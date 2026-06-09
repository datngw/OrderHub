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

    public async Task<(List<ProductListItem> Items, int TotalCount)> GetFilteredAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? search,
        bool? isActive,
        bool? inStock,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);
        if (inStock.HasValue)
            query = inStock.Value
                ? query.Where(p => p.Stock > 0)
                : query.Where(p => p.Stock == 0);

        query = ApplySorting(query, sortBy, sortOrder);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItem(
                p.Id, p.SKU, p.Name, p.Description,
                p.Price, p.Stock, p.Category, p.IsActive, p.CreatedAt))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public void Add(Product product) => context.Products.Add(product);

    public void Update(Product product) => context.Products.Update(product);

    public async Task<List<Product>> LockForUpdateAsync(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().OrderBy(id => id).ToList();
        return await context.Products
            .FromSqlInterpolated(
                $@"SELECT * FROM ""Products"" WHERE ""Id"" = ANY({ids}) ORDER BY ""Id"" FOR UPDATE")
            .ToListAsync(ct);
    }

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToList();
        return await context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> TryDecrementStockAsync(Guid productId, int quantity, CancellationToken ct)
    {
        // Single atomic UPDATE: only succeeds when product is available AND stock is sufficient.
        // No separate SELECT needed — the WHERE clause acts as the guard.
        // Returns 1 on success, 0 on failure (not found / deleted / inactive / insufficient stock).
        return await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "Products"
            SET    "Stock" = "Stock" - {quantity}
            WHERE  "Id"      = {productId}
              AND  "IsDeleted" = FALSE
              AND  "IsActive"  = TRUE
              AND  "Stock"    >= {quantity}
            """,
            ct);
    }

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
