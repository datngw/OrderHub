using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler(DbContext dbContext, ITokenService tokenService)
    : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await dbContext.Set<RefreshTokenEntity>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        if (existingToken is null)
            return Result<AuthResponse>.Failure(Error.Unauthorized("Invalid refresh token."));

        if (existingToken.IsRevoked)
            return Result<AuthResponse>.Failure(Error.Unauthorized("Refresh token has been revoked."));

        if (existingToken.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Failure(Error.Unauthorized("Refresh token has expired."));

        existingToken.IsRevoked = true;

        var newRefreshToken = new RefreshTokenEntity
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = existingToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        dbContext.Set<RefreshTokenEntity>().Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = existingToken.User;
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponse(accessToken, newRefreshToken.Token, user.Email, user.FullName, user.Role.ToString());
    }
}
