using Mapster;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace OrderHub.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock)
    : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken))
            return Result<AuthResponse>.Failure(AuthErrors.EmailAlreadyExists);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRoleEnum.Customer
        };

        userRepository.Add(user);

        var refreshToken = RefreshToken.Create(
            tokenService.GenerateRefreshToken(),
            user.Id,
            clock.GetUtcNow().AddDays(_jwtOptions.RefreshTokenDays));
        refreshTokenRepository.Add(refreshToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<AuthResponse>.Failure(AuthErrors.EmailAlreadyExists);
        }

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());

        return user.Adapt<AuthResponse>() with { AccessToken = accessToken, RefreshToken = refreshToken.Token };
    }
}
