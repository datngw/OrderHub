using Microsoft.EntityFrameworkCore;

using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(OrderHubDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);
    }

    public async Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken ct)
    {
        return await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);
    }

    public void Add(RefreshToken refreshToken) => context.RefreshTokens.Add(refreshToken);

    public void Update(RefreshToken refreshToken) => context.RefreshTokens.Update(refreshToken);
}
