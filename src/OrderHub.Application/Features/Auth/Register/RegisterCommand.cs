using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;

namespace OrderHub.Application.Features.Auth.Register;

public record RegisterCommand(string Email, string Password, string FullName)
    : ICommand<AuthResponse>;
