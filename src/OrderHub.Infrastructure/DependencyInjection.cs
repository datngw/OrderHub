using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Products;
using OrderHub.Domain.Users;
using OrderHub.Infrastructure.Persistence;
using OrderHub.Infrastructure.Persistence.Repositories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderHubDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
