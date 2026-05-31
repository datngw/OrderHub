using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .AsSplitQuery()
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
            .FromSqlInterpolated(
                $@"SELECT * FROM ""Orders"" WHERE ""Id"" = {id} FOR UPDATE")
            .Include(o => o.Items)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<TopProductRevenue>> GetTopProductsByRevenueAsync(
        DateTime? from, DateTime? to, int top, CancellationToken ct)
    {
        var fromDateUtc = from.HasValue
            ? (from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime())
            : (DateTime?)null;
        var toDateUtc = to.HasValue
            ? (to.Value.Kind == DateTimeKind.Utc ? to.Value.AddDays(1) : to.Value.AddDays(1).ToUniversalTime())
            : (DateTime?)null;

        var query = context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.Status != OrderStatusEnum.Cancelled);

        if (fromDateUtc.HasValue)
        {
            query = query.Where(oi => oi.Order.CreatedAt >= fromDateUtc.Value);
        }
        if (toDateUtc.HasValue)
        {
            query = query.Where(oi => oi.Order.CreatedAt < toDateUtc.Value);
        }

        var flatQuery = query.Select(oi => new
        {
            oi.ProductId,
            ProductName = oi.Product.Name,
            oi.Quantity,
            Revenue = oi.UnitPrice * oi.Quantity
        });

        var groupedQuery = flatQuery
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Revenue)
            })
            .OrderByDescending(r => r.TotalRevenue)
            .Take(top);

        var data = await groupedQuery.ToListAsync(ct);

        return data.Select(x => new TopProductRevenue(
            x.ProductId,
            x.ProductName,
            x.TotalQuantity,
            x.TotalRevenue
        )).ToList();
    }

    public async Task<List<RevenueByDay>> GetRevenueByDayAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var fromDateUtc = from.HasValue
            ? (from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime())
            : (DateTime?)null;
        var toDateUtc = to.HasValue
            ? (to.Value.Kind == DateTimeKind.Utc ? to.Value.AddDays(1) : to.Value.AddDays(1).ToUniversalTime())
            : (DateTime?)null;

        var query = context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatusEnum.Cancelled);

        if (fromDateUtc.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDateUtc.Value);
        }
        if (toDateUtc.HasValue)
        {
            query = query.Where(o => o.CreatedAt < toDateUtc.Value);
        }

        var data = await query
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToListAsync(ct);

        return data.Select(x => new RevenueByDay(
            new DateTime(x.Year, x.Month, x.Day, 0, 0, 0, DateTimeKind.Utc),
            x.OrderCount,
            x.TotalRevenue
        )).ToList();
    }

    public void Add(Order order) => context.Orders.Add(order);
}
