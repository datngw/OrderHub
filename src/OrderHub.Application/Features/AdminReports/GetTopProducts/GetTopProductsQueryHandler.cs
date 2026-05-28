using Mapster;
using Microsoft.Extensions.Caching.Memory;
using OrderHub.Application.Common.Caching;
using OrderHub.Application.Common.Messaging;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.AdminReports.GetTopProducts;

public sealed class GetTopProductsQueryHandler(IOrderRepository orderRepository, IMemoryCache cache)
    : IQueryHandler<GetTopProductsQuery, List<TopProductRevenueResponse>>
{
    public async Task<Result<List<TopProductRevenueResponse>>> Handle(
        GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        var version = cache.GetReportVersion();
        var cacheKey = CacheKeys.Reports.TopProducts(version, request.From, request.To, request.Top);

        var cached = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(3))
                .SetSize(1);

            var data = await orderRepository.GetTopProductsByRevenueAsync(
                request.From, request.To, request.Top, cancellationToken);

            return data.Adapt<List<TopProductRevenueResponse>>();
        });

        return Result<List<TopProductRevenueResponse>>.Success(cached!);
    }
}
