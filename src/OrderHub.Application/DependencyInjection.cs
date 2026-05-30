using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Common.Behaviors;

namespace OrderHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            cfg.AddOpenBehavior(typeof(ResultExceptionPipelineBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformancePipelineBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        return services;
    }
}
