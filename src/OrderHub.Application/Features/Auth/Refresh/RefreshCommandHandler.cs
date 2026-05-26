using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Users;
using Microsoft.Extensions.Options;

namespace OrderHub.Application.Features.Auth.Refresh;

public sealed class RefreshCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock)
    : ICommandHandler<RefreshCommand, AuthResponse>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenRepository.GetByTokenWithUserAsync(request.Token, cancellationToken);

        if (existingToken is null)
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);

        var now = clock.GetUtcNow();

        if (existingToken.IsRevoked)
        {
            await RevokeTokenFamilyAsync(existingToken, cancellationToken);
            return Result<AuthResponse>.Failure(AuthErrors.RefreshTokenRevoked);
        }

        if (existingToken.IsExpired(now))
            return Result<AuthResponse>.Failure(AuthErrors.RefreshTokenExpired);

        var newRefreshToken = RefreshToken.Create(
            tokenService.GenerateRefreshToken(),
            existingToken.UserId,
            now.AddDays(_jwtOptions.RefreshTokenDays));
        existingToken.Revoke(newRefreshToken.Id);

        refreshTokenRepository.Add(newRefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = existingToken.User;
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponse(accessToken, newRefreshToken.Token, user.Email, user.FullName, user.Role.ToString());
    }

    private async Task RevokeTokenFamilyAsync(RefreshToken revokedToken, CancellationToken cancellationToken)
    {
        var allUserTokens = await refreshTokenRepository.GetActiveTokensByUserIdAsync(revokedToken.UserId, cancellationToken);
        foreach (var token in allUserTokens)
        {
            token.Revoke();
        }
    }
}
