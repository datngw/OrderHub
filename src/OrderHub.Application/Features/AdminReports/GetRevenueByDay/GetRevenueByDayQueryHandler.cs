using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Application.Features.AdminReports.GetRevenueByDay;

public sealed class GetRevenueByDayQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetRevenueByDayQuery, List<RevenueByDayResponse>>
{
    public async Task<Result<List<RevenueByDayResponse>>> Handle(
        GetRevenueByDayQuery request, CancellationToken cancellationToken)
    {
        var data = await orderRepository.GetRevenueByDayAsync(
            request.From, request.To, cancellationToken);

        return Result<List<RevenueByDayResponse>>.Success(data.Adapt<List<RevenueByDayResponse>>());
    }
}
