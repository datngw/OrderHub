using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.AdminReports;

namespace OrderHub.Application.Features.AdminReports.GetTopProducts;

public record GetTopProductsQuery(DateTime? From = null, DateTime? To = null, int Top = 10)
    : IQuery<List<TopProductRevenueResponse>>;
