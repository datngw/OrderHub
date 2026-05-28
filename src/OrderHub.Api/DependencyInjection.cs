using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NetEscapades.AspNetCore.SecurityHeaders;
using OrderHub.Api.Common;
using OrderHub.Api.Middlewares;
using OrderHub.Application.Common.Security;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

namespace OrderHub.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrderHub API", Version = "v1" });
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

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Policies.AdminOnly, policy => policy.RequireRole(AuthorizationPolicies.Roles.Admin));

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

        return services;
    }

    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();

        app.UseSecurityHeaders(app.Services.GetRequiredService<HeaderPolicyCollection>());

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
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

        app.UseHttpsRedirection();
        app.UseHsts();

        app.UseResponseCompression();
        app.UseRateLimiter();
        app.UseRequestTimeouts();
        app.UseCors("Default");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapEndpointGroups();

        return app;
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
