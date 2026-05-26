using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Users;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (token is null)
            return Result.Failure(AuthErrors.InvalidRefreshToken);

        token.IsRevoked = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
