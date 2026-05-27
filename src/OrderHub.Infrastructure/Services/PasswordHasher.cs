using Microsoft.AspNetCore.Identity;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _inner = new PasswordHasher<User>();

    public string HashPassword(string password) => _inner.HashPassword(null!, password);

    public bool VerifyPassword(string password, string hashedPassword) =>
        _inner.VerifyHashedPassword(null!, hashedPassword, password) == PasswordVerificationResult.Success;
}
