using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderHub.Application.Common.Security;

namespace OrderHub.UnitTests.Shared;

public static class MockHelpers
{
    public static IMemoryCache CreateMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
    }

    public static IOptions<JwtOptions> CreateJwtOptions()
    {
        return Options.Create(new JwtOptions
        {
            Key = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "OrderHub.Tests",
            Audience = "OrderHub.Tests",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
    }
}
