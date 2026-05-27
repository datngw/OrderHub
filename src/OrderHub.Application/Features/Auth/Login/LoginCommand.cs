using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;

namespace OrderHub.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password)
    : ICommand<AuthResponse>;
