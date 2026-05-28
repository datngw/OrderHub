namespace OrderHub.Application.Features.AdminReports;

public record TopProductRevenueResponse(
    Guid ProductId,
    string ProductName,
    int TotalQuantity,
    decimal TotalRevenue);
