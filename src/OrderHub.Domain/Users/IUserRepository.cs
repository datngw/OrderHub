namespace OrderHub.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    void Add(User user);
}
