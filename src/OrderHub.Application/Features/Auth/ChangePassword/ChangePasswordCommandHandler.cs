using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<ChangePasswordCommandHandler> logger)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(AuthErrors.UserNotFound);

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(AuthErrors.IncorrectCurrentPassword);

        user.UpdatePasswordHash(passwordHasher.HashPassword(request.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password changed successfully for user {Email}", user.Email);

        return Result.Success();
    }
}
