using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email)
    : ICommand;
