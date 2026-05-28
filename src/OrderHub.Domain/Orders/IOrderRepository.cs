namespace OrderHub.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
    Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct);
    void Add(Order order);
}
