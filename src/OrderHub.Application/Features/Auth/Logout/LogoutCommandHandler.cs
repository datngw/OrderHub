using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Domain.Users;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler(DbContext dbContext)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await dbContext.Set<RefreshTokenEntity>()
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (token is null)
            return Result.Failure(Error.Unauthorized("Invalid refresh token."));

        token.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
