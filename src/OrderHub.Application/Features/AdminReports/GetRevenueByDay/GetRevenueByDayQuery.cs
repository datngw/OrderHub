using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.AdminReports;

namespace OrderHub.Application.Features.AdminReports.GetRevenueByDay;

public record GetRevenueByDayQuery(DateTime? From = null, DateTime? To = null)
    : IQuery<List<RevenueByDayResponse>>;
