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

    public async Task<List<TopProductRevenue>> GetTopProductsByRevenueAsync(
        DateTime? from, DateTime? to, int top, CancellationToken ct)
    {
        var query = context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => !from.HasValue || oi.Order.CreatedAt >= from.Value)
            .Where(oi => !to.HasValue || oi.Order.CreatedAt < to.Value.AddDays(1))
            .Where(oi => oi.Order.Status != OrderStatusEnum.Cancelled)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new TopProductRevenue(
                g.Key.ProductId,
                g.Key.Name,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.UnitPrice * oi.Quantity)))
            .OrderByDescending(x => x.TotalRevenue)
            .Take(top);

        return await query.AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<RevenueByDay>> GetRevenueByDayAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = context.Orders
            .Where(o => o.Status != OrderStatusEnum.Cancelled)
            .Where(o => !from.HasValue || o.CreatedAt >= from.Value)
            .Where(o => !to.HasValue || o.CreatedAt < to.Value.AddDays(1));

        var result = await query
            .AsNoTracking()
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new RevenueByDay(
                g.Key,
                g.Count(),
                g.Sum(o => o.TotalAmount)))
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return result;
    }

    public void Add(Order order) => context.Orders.Add(order);
}
