using Microsoft.Extensions.Logging;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<ResetPasswordCommandHandler> logger)
    : ICommandHandler<ResetPasswordCommand>
{
    private const string HardcodedResetCode = "123456";

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null)
            return Result.Failure(AuthErrors.UserNotFound);

        if (request.Code != HardcodedResetCode)
            return Result.Failure(AuthErrors.InvalidResetCode);

        user.UpdatePasswordHash(passwordHasher.HashPassword(request.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password reset successfully for user {Email}", user.Email);

        return Result.Success();
    }
}
