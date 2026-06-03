using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Endpoints.Baskets.Requests;
using OrderHub.Application.Features.Baskets;
using OrderHub.Application.Features.Baskets.AddBasketItem;
using OrderHub.Application.Features.Baskets.ClearBasket;
using OrderHub.Application.Features.Baskets.GetBasket;
using OrderHub.Application.Features.Baskets.RemoveBasketItem;
using OrderHub.Application.Features.Baskets.UpdateBasketItem;

namespace OrderHub.Api.Endpoints.Baskets;

public sealed class BasketEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet("basket")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/basket")
            .WithApiVersionSet(versionSet)
            .WithTags("Basket")
            .RequireAuthorization();

        group.MapGet("/", HandleGetBasket)
            .WithName("GetBasket").WithSummary("Get current user's basket")
            .HasApiVersion(new ApiVersion(1))
            .Produces<BasketResponse>();

        group.MapPost("/items", HandleAddBasketItem)
            .WithName("AddBasketItem").WithSummary("Add a product to the basket")
            .HasApiVersion(new ApiVersion(1))
            .Produces<BasketResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/items/{productId:guid}", HandleUpdateBasketItem)
            .WithName("UpdateBasketItem").WithSummary("Update item quantity in the basket")
            .HasApiVersion(new ApiVersion(1))
            .Produces<BasketResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/items/{productId:guid}", HandleRemoveBasketItem)
            .WithName("RemoveBasketItem").WithSummary("Remove an item from the basket")
            .HasApiVersion(new ApiVersion(1))
            .Produces<BasketResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/", HandleClearBasket)
            .WithName("ClearBasket").WithSummary("Clear the entire basket")
            .HasApiVersion(new ApiVersion(1))
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> HandleGetBasket(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBasketQuery(), ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleAddBasketItem(
        [FromBody] AddBasketItemRequest request, IMediator mediator, CancellationToken ct)
    {
        var command = new AddBasketItemCommand(request.ProductId, request.Quantity);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleUpdateBasketItem(
        Guid productId, [FromBody] UpdateBasketItemRequest request, IMediator mediator, CancellationToken ct)
    {
        var command = new UpdateBasketItemCommand(productId, request.Quantity);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleRemoveBasketItem(
        Guid productId, IMediator mediator, CancellationToken ct)
    {
        var command = new RemoveBasketItemCommand(productId);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleClearBasket(
        IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new ClearBasketCommand(), ct);
        return Results.NoContent();
    }
}
