namespace OrderHub.Application.Features.AdminReports;

public record RevenueByDayResponse(
    DateTime Date,
    int OrderCount,
    decimal TotalRevenue);
