using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.AdminReports.GetTopProducts;

public sealed class GetTopProductsQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetTopProductsQuery, List<TopProductRevenueResponse>>
{
    public async Task<Result<List<TopProductRevenueResponse>>> Handle(
        GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        var data = await orderRepository.GetTopProductsByRevenueAsync(
            request.From, request.To, request.Top, cancellationToken);

        return Result<List<TopProductRevenueResponse>>.Success(data.Adapt<List<TopProductRevenueResponse>>());
    }
}
