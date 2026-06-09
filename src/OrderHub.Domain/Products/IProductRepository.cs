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
        bool? isActive,
        bool? inStock,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken ct);
    void Add(Product product);
    void Update(Product product);
    Task<List<Product>> LockForUpdateAsync(IEnumerable<Guid> productIds, CancellationToken ct);

    /// <summary>
    /// Fetches products by IDs without acquiring any row-level lock.
    /// Use this for pre-validation and price snapshot before the atomic decrement.
    /// </summary>
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct);

    /// <summary>
    /// Atomically decrements the stock of a product by <paramref name="quantity"/> if and only if
    /// the product exists, is not deleted, is active, and has sufficient stock.
    /// Returns the number of rows affected (1 = success, 0 = failed — check stock/availability).
    /// Must be called inside an open database transaction.
    /// </summary>
    Task<int> TryDecrementStockAsync(Guid productId, int quantity, CancellationToken ct);
}
