using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Common;
using OrderHub.Application.Common.Security;
using OrderHub.Api.Endpoints.Products.Requests;
using OrderHub.Application.Common.Pagination;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Products.CreateProduct;
using OrderHub.Application.Features.Products.DeleteProduct;
using OrderHub.Application.Features.Products.GetProductById;
using OrderHub.Application.Features.Products.GetProducts;
using OrderHub.Application.Features.Products.UpdateProduct;

namespace OrderHub.Api.Endpoints.Products;

public sealed class ProductEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet("products")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/products")
            .WithApiVersionSet(versionSet)
            .WithTags("Products")
            .RequireRateLimiting("products");

        group.MapGet("/", HandleGetProducts)
            .WithName("GetProducts").WithSummary("Get paginated product list with filters")
            .HasApiVersion(new ApiVersion(1))
            .Produces<PagedResult<ProductResponse>>();

        group.MapGet("/{id:guid}", HandleGetProduct)
            .WithName("GetProduct").WithSummary("Get product by ID")
            .HasApiVersion(new ApiVersion(1))
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", HandleCreateProduct)
            .WithName("CreateProduct").WithSummary("Create a new product")
            .HasApiVersion(new ApiVersion(1))
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.Policies.AdminOnly);

        group.MapPut("/{id:guid}", HandleUpdateProduct)
            .WithName("UpdateProduct").WithSummary("Update an existing product")
            .HasApiVersion(new ApiVersion(1))
            .Produces<ProductResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Policies.AdminOnly);

        group.MapDelete("/{id:guid}", HandleDeleteProduct)
            .WithName("DeleteProduct").WithSummary("Soft delete a product")
            .HasApiVersion(new ApiVersion(1))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Policies.AdminOnly);
    }

    private static async Task<Results<Ok<PagedResult<ProductResponse>>, CustomProblemResult>> HandleGetProducts(
        [AsParameters] GetProductsQuery query, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.ToResponse();
    }

    private static async Task<Results<Ok<ProductResponse>, CustomProblemResult>> HandleGetProduct(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), ct);
        return result.ToResponse();
    }

    private static async Task<Results<Created<ProductResponse>, CustomProblemResult>> HandleCreateProduct(
        [FromBody] CreateProductRequest request, IMediator mediator, CancellationToken ct)
    {
        var command = new CreateProductCommand(request.SKU, request.Name, request.Description, request.Price, request.Stock, request.Category);
        var result = await mediator.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/products/{result.Value?.Id}");
    }

    private static async Task<Results<Ok<ProductResponse>, CustomProblemResult>> HandleUpdateProduct(
        Guid id, [FromBody] UpdateProductRequest request, IMediator mediator, CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Stock, request.Category);
        var result = await mediator.Send(command, ct);
        return result.ToResponse();
    }

    private static async Task<Results<NoContent, CustomProblemResult>> HandleDeleteProduct(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(id), ct);
        return result.ToNoContentResponse();
    }
}
