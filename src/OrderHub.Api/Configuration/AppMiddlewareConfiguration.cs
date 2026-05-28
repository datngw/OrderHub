using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NetEscapades.AspNetCore.SecurityHeaders;
using OrderHub.Api.Common;
using Scalar.AspNetCore;
using Serilog;

namespace OrderHub.Api;

public static class AppMiddlewareConfiguration
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        UseSecurityAndErrorHandling(app);
        UseSwaggerIfDevelopment(app);
        UseMiddlewarePipeline(app);
        UseAuthenticationAndAuthorization(app);
        UseHealthChecks(app);

        app.MapEndpointGroups();

        return app;
    }

    private static void UseSecurityAndErrorHandling(WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseSecurityHeaders(app.Services.GetRequiredService<HeaderPolicyCollection>());
        app.UseExceptionHandler();
    }

    private static void UseSwaggerIfDevelopment(WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.UseSwagger();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("OrderHub API")
                .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .AddPreferredSecuritySchemes(["Bearer"])
                .AddDocument("v1", routePattern: "/swagger/v1/swagger.json");
        });
    }

    private static void UseMiddlewarePipeline(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseHsts();
        app.UseResponseCompression();
        app.UseRateLimiter();
        app.UseRequestTimeouts();
        app.UseCors("Default");
    }

    private static void UseAuthenticationAndAuthorization(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static void UseHealthChecks(WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
    }

    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] [{MachineName}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}
