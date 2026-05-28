using System.Reflection;
using OrderHub.Application.Common.Security;

namespace OrderHub.Api.Common;

public sealed class SanitizeHtmlEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null) continue;
            SanitizeObjectStrings(arg);
        }

        return await next(context);
    }

    private static void SanitizeObjectStrings(object obj)
    {
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType == typeof(string) && prop.CanWrite)
            {
                var value = (string?)prop.GetValue(obj);
                if (value is not null)
                    prop.SetValue(obj, HtmlInputSanitizer.Sanitize(value));
            }
        }
    }
}
