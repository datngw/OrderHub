using Microsoft.Extensions.Caching.Memory;

namespace OrderHub.Application.Common.Caching;

public static class CacheKeys
{
    private const string ProductVersionKey = "products:version";
    private const string ReportVersionKey = "reports:version";

    public static class Products
    {
        public const string ByIdPrefix = "products:byid";

        public static string ById(Guid id) => $"{ByIdPrefix}:{id}";

        public static string List(string version, int page, int pageSize, string? category,
            decimal? minPrice, decimal? maxPrice, string? search,
            string? sortBy, string? sortOrder) =>
            $"products:list:v{version}:{page}:{pageSize}:{category}:{minPrice}:{maxPrice}:{search}:{sortBy}:{sortOrder}";
    }

    public static class Reports
    {
        public static string TopProducts(string version, DateTime? from, DateTime? to, int top) =>
            $"reports:top-products:v{version}:{from}:{to}:{top}";

        public static string RevenueByDay(string version, DateTime? from, DateTime? to) =>
            $"reports:revenue-by-day:v{version}:{from}:{to}";
    }

    public static string GetProductVersion(this IMemoryCache cache) =>
        cache.GetOrCreate(ProductVersionKey, entry =>
        {
            entry.SetPriority(CacheItemPriority.NeverRemove)
                .SetSize(1);
            return Guid.NewGuid().ToString("N")[..8];
        })!;

    public static string GetReportVersion(this IMemoryCache cache) =>
        cache.GetOrCreate(ReportVersionKey, entry =>
        {
            entry.SetPriority(CacheItemPriority.NeverRemove)
                .SetSize(1);
            return Guid.NewGuid().ToString("N")[..8];
        })!;

    public static void InvalidateProducts(this IMemoryCache cache, Guid? productId = null)
    {
        if (productId.HasValue)
            cache.Remove(Products.ById(productId.Value));

        cache.Remove(ProductVersionKey);
    }

    public static void InvalidateReports(this IMemoryCache cache)
    {
        cache.Remove(ReportVersionKey);
    }
}
