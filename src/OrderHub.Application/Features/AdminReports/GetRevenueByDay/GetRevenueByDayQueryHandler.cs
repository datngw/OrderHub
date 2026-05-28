using Mapster;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.AdminReports.GetRevenueByDay;

public sealed class GetRevenueByDayQueryHandler(IOrderRepository orderRepository, IMemoryCache cache)
    : IQueryHandler<GetRevenueByDayQuery, List<RevenueByDayResponse>>
{
    public async Task<Result<List<RevenueByDayResponse>>> Handle(
        GetRevenueByDayQuery request, CancellationToken cancellationToken)
    {
        var version = cache.GetReportVersion();
        var cacheKey = CacheKeys.Reports.RevenueByDay(version, request.From, request.To);

        var cached = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(3))
                .SetSize(1);

            var data = await orderRepository.GetRevenueByDayAsync(
                request.From, request.To, cancellationToken);

            return data.Adapt<List<RevenueByDayResponse>>();
        });

        return Result<List<RevenueByDayResponse>>.Success(cached!);
    }
}
