using System.Reflection;
using OrderHub.Api.Common;

namespace OrderHub.Api.Common;

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
}
