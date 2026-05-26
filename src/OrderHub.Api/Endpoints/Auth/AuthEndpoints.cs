using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Common;
using OrderHub.Api.Features.Auth.Requests;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Features.Auth.Login;
using OrderHub.Application.Features.Auth.Logout;
using OrderHub.Application.Features.Auth.RefreshToken;
using OrderHub.Application.Features.Auth.Register;

namespace OrderHub.Api.Features.Auth;

public sealed class AuthEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth").WithOpenApi();

        group.MapPost("/register", static async ([FromBody] RegisterRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName), ct);
            return result.ToResponse();
        })
        .WithName("Register").WithSummary("Register a new user")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem();

        group.MapPost("/login", static async ([FromBody] LoginRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
            return result.ToResponse();
        })
        .WithName("Login").WithSummary("Authenticate and get tokens")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", static async ([FromBody] RefreshTokenRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return result.ToResponse();
        })
        .WithName("RefreshToken").WithSummary("Refresh access token")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", static async ([FromBody] RefreshTokenRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LogoutCommand(request.RefreshToken), ct);
            return result.ToNoContentResponse();
        })
        .WithName("Logout").WithSummary("Revoke refresh token")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
