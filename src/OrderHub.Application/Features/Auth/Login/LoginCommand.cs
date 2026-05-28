using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password)
    : ICommand<AuthResponse>;
