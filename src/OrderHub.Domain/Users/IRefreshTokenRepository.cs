namespace OrderHub.Domain.Users;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken ct);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken ct);
    void Add(RefreshToken refreshToken);
    void Update(RefreshToken refreshToken);
}
