using Microsoft.EntityFrameworkCore;

using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public class UserRepository(OrderHubDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Users.FindAsync([id], ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
    {
        return await context.Users.AnyAsync(u => u.Email == email, ct);
    }

    public void Add(User user) => context.Users.Add(user);
}
