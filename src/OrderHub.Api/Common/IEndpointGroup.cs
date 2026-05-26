using Microsoft.AspNetCore.Routing;

namespace OrderHub.Api.Common;

public interface IEndpointGroup
{
    static abstract void MapGroup(IEndpointRouteBuilder endpoints);
}
