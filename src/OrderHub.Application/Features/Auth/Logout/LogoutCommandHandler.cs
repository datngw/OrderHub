using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<LogoutCommandHandler> logger)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (token is null)
        {
            logger.LogWarning("Logout failed: refresh token not found");
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (token.IsRevoked)
        {
            logger.LogInformation("Logout: token was already revoked");
            return Result.Success();
        }

        token.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User logged out, token revoked");

        return Result.Success();
    }
}
