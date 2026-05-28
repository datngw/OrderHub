using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderHub.Application.Common.Security;

namespace OrderHub.Infrastructure.Services;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User
                ?? throw new InvalidOperationException("No HttpContext available.");

            var claim = user.FindFirst(JwtRegisteredClaimNames.Sub)
                        ?? user.FindFirst(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("No user ID claim found.");

            return Guid.Parse(claim.Value);
        }
    }

    public bool IsAdmin =>
        httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
