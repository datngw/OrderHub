using Mapster;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Common;
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
    IDateTimeProvider clock,
    ILogger<LoginCommandHandler> logger)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed for {Email}: invalid credentials", request.Email);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = RefreshToken.Create(
            tokenService.GenerateRefreshToken(),
            user.Id,
            clock.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        refreshTokenRepository.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Result<AuthResponse>.Success(user.Adapt<AuthResponse>() with { AccessToken = accessToken, RefreshToken = refreshToken.Token });
    }
}
