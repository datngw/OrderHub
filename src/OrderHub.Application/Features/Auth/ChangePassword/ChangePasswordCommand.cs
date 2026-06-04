using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : ICommand;
