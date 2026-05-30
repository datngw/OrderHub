using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Endpoints;
using OrderHub.Api.Endpoints.Auth.Requests;
using OrderHub.Api.Extensions;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Features.Auth.Login;
using OrderHub.Domain.Users;
using OrderHub.Application.Features.Auth.Logout;
using OrderHub.Application.Features.Auth.Refresh;
using OrderHub.Application.Features.Auth.Register;

namespace OrderHub.Api.Endpoints.Auth;

public sealed class AuthEndpoints : IEndpointGroup
{
    public static void MapGroup(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet("auth")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .WithTags("Auth")
            .WithHtmlSanitization();

        group.MapPost("/register", HandleRegister)
            .WithName("Register").WithSummary("Register a new user")
            .HasApiVersion(new ApiVersion(1))
            .AllowAnonymous()
            .RequireRateLimiting("auth-register")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/login", HandleLogin)
            .WithName("Login").WithSummary("Authenticate and get tokens")
            .HasApiVersion(new ApiVersion(1))
            .AllowAnonymous()
            .RequireRateLimiting("auth-login")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/refresh", HandleRefresh)
            .WithName("RefreshToken").WithSummary("Refresh access token")
            .HasApiVersion(new ApiVersion(1))
            .AllowAnonymous()
            .RequireRateLimiting("auth-refresh")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", HandleLogout)
            .WithName("Logout").WithSummary("Revoke refresh token")
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .RequireRateLimiting("products")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleRegister(
        [FromBody] RegisterRequest request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName), ct);
        return Results.Created("/api/v1/auth/login", result.Value);
    }

    private static async Task<IResult> HandleLogin(
        [FromBody] LoginRequest request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleRefresh(
        [FromBody] RefreshTokenRequest request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshCommand(request.RefreshToken), ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleLogout(
        [FromBody] RefreshTokenRequest request, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return Results.NoContent();
    }
}
