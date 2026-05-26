using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Common;
using OrderHub.Api.Features.Products.Requests;
using OrderHub.Application.Common.Pagination;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Products.CreateProduct;
using OrderHub.Application.Features.Products.DeleteProduct;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.GetProductById;
using OrderHub.Application.Features.Products.GetProducts;
using OrderHub.Application.Features.Products.UpdateProduct;

namespace OrderHub.Api.Features.Products;

public sealed class ProductEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products").WithOpenApi();

        group.MapGet("/", static async ([AsParameters] GetProductsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return result.ToResponse();
        })
        .WithName("GetProducts").WithSummary("Get paginated product list with filters")
        .Produces<PagedResult<ProductResponse>>();

        group.MapGet("/{id:guid}", static async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return result.ToResponse();
        })
        .WithName("GetProduct").WithSummary("Get product by ID")
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", static async ([FromBody] CreateProductRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateProductCommand(request.SKU, request.Name, request.Description, request.Price, request.Stock, request.Category);
            var result = await mediator.Send(command, ct);
            return result.ToCreatedResponse($"/api/products/{result.Value?.Id}");
        })
        .WithName("CreateProduct").WithSummary("Create a new product")
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", static async (Guid id, [FromBody] UpdateProductRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Stock, request.Category);
            var result = await mediator.Send(command, ct);
            return result.ToResponse();
        })
        .WithName("UpdateProduct").WithSummary("Update an existing product")
        .Produces<ProductResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", static async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteProductCommand(id), ct);
            return result.ToNoContentResponse();
        })
        .WithName("DeleteProduct").WithSummary("Soft delete a product")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("AdminOnly");
    }
}
