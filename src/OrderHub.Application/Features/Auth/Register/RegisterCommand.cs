using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Auth;

namespace OrderHub.Application.Features.Auth.Register;

public record RegisterCommand(string Email, string Password, string FullName)
    : ICommand<AuthResponse>;
