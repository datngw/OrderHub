namespace OrderHub.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct);
    Task<(List<ProductListItem> Items, int TotalCount)> GetFilteredAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? search,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken ct);
    void Add(Product product);
    void Update(Product product);
    Task<List<Product>> LockForUpdateAsync(IEnumerable<Guid> productIds, CancellationToken ct);
}
