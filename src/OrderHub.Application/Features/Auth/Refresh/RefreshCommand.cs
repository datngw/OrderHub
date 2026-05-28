using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.Refresh;

public record RefreshCommand(string Token)
    : ICommand<AuthResponse>;
