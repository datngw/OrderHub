using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Auth;
using OrderHub.Infrastructure.Persistence;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderHubDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OrderHubDbContext>());

        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
