using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.Register;

public record RegisterCommand(string Email, string Password, string FullName)
    : ICommand<AuthResponse>;
