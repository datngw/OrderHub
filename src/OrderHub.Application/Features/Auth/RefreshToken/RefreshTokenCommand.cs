using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Features.Auth;

namespace OrderHub.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string Token)
    : ICommand<AuthResponse>;
