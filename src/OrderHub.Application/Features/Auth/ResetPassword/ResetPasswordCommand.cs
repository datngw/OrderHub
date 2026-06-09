using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Code, string NewPassword)
    : ICommand;
