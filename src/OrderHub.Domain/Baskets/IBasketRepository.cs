namespace OrderHub.Domain.Baskets;

public interface IBasketRepository
{
    Task<Basket?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task SetAsync(Basket basket, CancellationToken ct);
    Task RemoveAsync(Guid userId, CancellationToken ct);
}
