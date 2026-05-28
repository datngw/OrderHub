namespace OrderHub.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
    Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct);
    Task<List<TopProductRevenue>> GetTopProductsByRevenueAsync(
        DateTime? from, DateTime? to, int top, CancellationToken ct);
    Task<List<RevenueByDay>> GetRevenueByDayAsync(
        DateTime? from, DateTime? to, CancellationToken ct);
    void Add(Order order);
}

public record TopProductRevenue(Guid ProductId, string ProductName, int TotalQuantity, decimal TotalRevenue);

public record RevenueByDay(DateTime Date, int OrderCount, decimal TotalRevenue);
