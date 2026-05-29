using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NetEscapades.AspNetCore.SecurityHeaders;
using OrderHub.Api.Common;
using OrderHub.Api.Logging;
using OrderHub.Api.Middlewares;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace OrderHub.Api;

public static class AppMiddlewareConfiguration
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        UseRequestLogging(app);           // Outermost — sees final status code after exception handling
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
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseHttpsRedirection();
        app.UseHsts();
        app.UseResponseCompression();
        app.UseRateLimiter();
        app.UseRequestTimeouts();
        app.UseCors("Default");
    }

    private static void UseRequestLogging(WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("UserId",
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                    return LogEventLevel.Verbose;

                return elapsed > 500 ? LogEventLevel.Warning : LogEventLevel.Information;
            };
        });
    }

    private static void UseAuthenticationAndAuthorization(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static void UseHealthChecks(WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
    }

    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration);
            loggerConfig.Destructure.With<SensitiveDataDestructuringPolicy>();
            loggerConfig.Filter.With<SensitiveLogEventFilter>();
        });

        return builder;
    }
}
