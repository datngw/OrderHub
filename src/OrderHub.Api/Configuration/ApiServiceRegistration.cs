using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi;
using NetEscapades.AspNetCore.SecurityHeaders;
using OrderHub.Api.Middlewares;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

namespace OrderHub.Api;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        AddErrorHandling(services);
        AddSwagger(services);
        AddApiVersioning(services);
        AddRateLimiting(services);
        AddCors(services, configuration);
        AddSecurityAndCompression(services, configuration);
        return services;
    }

    private static void AddErrorHandling(IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OrderHub API",
                Version = "v1",
                Description = "Central order management API for an e-commerce platform. Provides product catalog management, order processing with concurrency control, authentication, and admin reporting."
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });
        });
    }

    private static void AddApiVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("auth-login", context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 5,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("auth-register", context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 3,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("auth-refresh", context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("products", context =>
            {
                var key = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("orders", context =>
            {
                var key = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("admin", context =>
            {
                var key = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 40,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    AutoReplenishment = true
                });
            });
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:3000"];

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private static void AddSecurityAndCompression(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!, name: "postgresql", tags: ["ready"]);

        services.AddRequestTimeouts();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        var securityHeadersPolicy = new HeaderPolicyCollection()
            .AddContentTypeOptionsNoSniff()
            .AddFrameOptionsDeny()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddContentSecurityPolicy(builder =>
            {
                builder.AddDefaultSrc().Self();
                builder.AddScriptSrc().Self().From("https://cdn.jsdelivr.net").UnsafeInline();
                builder.AddStyleSrc().Self().From("https://cdn.jsdelivr.net").UnsafeInline();
                builder.AddImgSrc().Self().From("data:");
                builder.AddFontSrc().Self().From("https://cdn.jsdelivr.net");
                builder.AddConnectSrc().Self();
            })
            .RemoveServerHeader();

        services.AddSingleton(securityHeadersPolicy);
    }
}
