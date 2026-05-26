using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Results;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Common.Persistence;

using OrderHub.Domain.Users;
using RefreshTokenEntity = OrderHub.Domain.Users.RefreshToken;

namespace OrderHub.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork, ITokenService tokenService)
    : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken))
            return Result<AuthResponse>.Failure(AuthErrors.EmailAlreadyExists);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRoleEnum.Customer
        };

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = new RefreshTokenEntity
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        refreshTokenRepository.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken.Token, user.Email, user.FullName, user.Role.ToString());
    }
}
