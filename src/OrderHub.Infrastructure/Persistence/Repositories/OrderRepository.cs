using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Orders;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public class OrderRepository(OrderHubDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        return await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public void Add(Order order) => context.Orders.Add(order);
}
