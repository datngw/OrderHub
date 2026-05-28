using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Common;
using OrderHub.Application.Common.Security;
using OrderHub.Api.Endpoints.Orders.Requests;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.CancelOrder;
using OrderHub.Application.Features.Orders.CreateOrder;
using OrderHub.Application.Features.Orders.GetMyOrders;
using OrderHub.Application.Features.Orders.GetOrderById;
using OrderHub.Application.Features.Orders.UpdateOrderStatus;
namespace OrderHub.Api.Endpoints.Orders;

public sealed class OrderEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet("orders")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/orders")
            .WithApiVersionSet(versionSet)
            .WithTags("Orders")
            .RequireRateLimiting("orders")
            .RequireAuthorization();

        group.MapPost("/", HandleCreateOrder)
            .WithName("CreateOrder").WithSummary("Create a new order")
            .HasApiVersion(new ApiVersion(1))
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/me", HandleGetMyOrders)
            .WithName("GetMyOrders").WithSummary("Get current user's order history")
            .HasApiVersion(new ApiVersion(1))
            .Produces<PagedResult<OrderResponse>>();

        group.MapGet("/{id:guid}", HandleGetOrderById)
            .WithName("GetOrderById").WithSummary("Get order by ID (owner or admin)")
            .HasApiVersion(new ApiVersion(1))
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/status", HandleUpdateOrderStatus)
            .WithName("UpdateOrderStatus").WithSummary("Update order status (Admin only)")
            .HasApiVersion(new ApiVersion(1))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthorizationPolicies.Policies.AdminOnly);

        group.MapPost("/{id:guid}/cancel", HandleCancelOrder)
            .WithName("CancelOrder").WithSummary("Cancel a pending order (owner or admin)")
            .HasApiVersion(new ApiVersion(1))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Created<OrderResponse>, CustomProblemResult>> HandleCreateOrder(
        [FromBody] CreateOrderRequest request, IMediator mediator, CancellationToken ct)
    {
        var command = new CreateOrderCommand(
            request.Items.Select(i => new CreateOrderItem(i.ProductId, i.Quantity)).ToList());
        var result = await mediator.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/orders/{result.Value?.Id}");
    }

    private static async Task<Results<Ok<PagedResult<OrderResponse>>, CustomProblemResult>> HandleGetMyOrders(
        IMediator mediator, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyOrdersQuery(page, pageSize), ct);
        return result.ToResponse();
    }

    private static async Task<Results<Ok<OrderResponse>, CustomProblemResult>> HandleGetOrderById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return result.ToResponse();
    }

    private static async Task<Results<NoContent, CustomProblemResult>> HandleUpdateOrderStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, request.Status), ct);
        return result.ToNoContentResponse();
    }

    private static async Task<Results<NoContent, CustomProblemResult>> HandleCancelOrder(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(id), ct);
        return result.ToNoContentResponse();
    }
}
