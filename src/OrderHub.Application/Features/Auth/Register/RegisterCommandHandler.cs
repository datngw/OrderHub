using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Users;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler(DbContext dbContext, ITokenService tokenService)
    : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.Set<User>().AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken))
            return Result<AuthResponse>.Failure(Error.Conflict("A user with this email already exists."));

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Customer
        };

        dbContext.Set<User>().Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = new RefreshTokenEntity
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        dbContext.Set<RefreshTokenEntity>().Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken.Token, user.Email, user.FullName, user.Role.ToString());
    }
}
