using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Common.Persistence;
using OrderHub.Domain.Users;
using Microsoft.Extensions.Options;

namespace OrderHub.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = RefreshToken.Create(
            tokenService.GenerateRefreshToken(),
            user.Id,
            clock.GetUtcNow().AddDays(_jwtOptions.RefreshTokenDays));

        refreshTokenRepository.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken.Token, user.Email, user.FullName, user.Role.ToString());
    }
}
