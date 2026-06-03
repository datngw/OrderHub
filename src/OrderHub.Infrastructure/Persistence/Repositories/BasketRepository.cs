using Microsoft.Extensions.Caching.Memory;
using OrderHub.Domain.Baskets;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public sealed class BasketRepository(IMemoryCache cache) : IBasketRepository
{
    private static string CacheKey(Guid userId) => $"basket:{userId}";

    public Task<Basket?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        cache.TryGetValue(CacheKey(userId), out Basket? basket);
        return Task.FromResult(basket);
    }

    public Task SetAsync(Basket basket, CancellationToken ct)
    {
        cache.Set(CacheKey(basket.UserId), basket, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Size = 1
        });

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, CancellationToken ct)
    {
        cache.Remove(CacheKey(userId));
        return Task.CompletedTask;
    }
}
