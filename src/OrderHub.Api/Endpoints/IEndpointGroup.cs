using Microsoft.AspNetCore.Routing;

namespace OrderHub.Api.Endpoints;

public interface IEndpointGroup
{
    static abstract void MapGroup(IEndpointRouteBuilder endpoints);
}
