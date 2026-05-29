using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OrderHub.Api.Endpoints;
using OrderHub.Api.Extensions;
using OrderHub.Api.Middlewares;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.AdminReports;
using OrderHub.Application.Features.AdminReports.GetRevenueByDay;
using OrderHub.Application.Features.AdminReports.GetTopProducts;

namespace OrderHub.Api.Endpoints.Admin;

public sealed class AdminReportEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet("admin-reports")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/admin/reports")
            .WithApiVersionSet(versionSet)
            .WithTags("Admin Reports")
            .RequireRateLimiting("admin")
            .RequireAuthorization(AuthorizationPolicies.Policies.AdminOnly);

        group.MapGet("/top-products", HandleGetTopProducts)
            .WithName("GetTopProducts").WithSummary("Top products by revenue")
            .HasApiVersion(new ApiVersion(1))
            .Produces<List<TopProductRevenueResponse>>();

        group.MapGet("/revenue-by-day", HandleGetRevenueByDay)
            .WithName("GetRevenueByDay").WithSummary("Revenue aggregated by day")
            .HasApiVersion(new ApiVersion(1))
            .Produces<List<RevenueByDayResponse>>();
    }

    private static async Task<Results<Ok<List<TopProductRevenueResponse>>, CustomProblemResult>> HandleGetTopProducts(
        IMediator mediator, DateTime? from = null, DateTime? to = null, int top = 10, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetTopProductsQuery(from, to, top), ct);
        return result.ToResponse();
    }

    private static async Task<Results<Ok<List<RevenueByDayResponse>>, CustomProblemResult>> HandleGetRevenueByDay(
        IMediator mediator, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetRevenueByDayQuery(from, to), ct);
        return result.ToResponse();
    }
}
