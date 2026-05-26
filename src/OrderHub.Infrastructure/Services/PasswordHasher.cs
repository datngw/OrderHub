using OrderHub.Application.Features.Auth;

namespace OrderHub.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}
