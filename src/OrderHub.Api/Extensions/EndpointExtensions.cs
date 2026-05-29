using System.Reflection;
using OrderHub.Api.Endpoints;
using OrderHub.Api.Filters;

namespace OrderHub.Api.Extensions;

public static class EndpointExtensions
{
    public static WebApplication MapEndpointGroups(this WebApplication app)
    {
        var endpointGroupType = typeof(IEndpointGroup);

        foreach (var type in typeof(EndpointExtensions).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces().Contains(endpointGroupType)))
        {
            var mapMethod = type.GetMethod("MapGroup", BindingFlags.Public | BindingFlags.Static);
            mapMethod?.Invoke(null, [app]);
        }

        return app;
    }

    public static RouteGroupBuilder WithHtmlSanitization(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<SanitizeHtmlEndpointFilter>();
        return group;
    }
}
