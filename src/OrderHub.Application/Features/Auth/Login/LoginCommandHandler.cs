using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Users;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(DbContext dbContext, ITokenService tokenService)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);

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
