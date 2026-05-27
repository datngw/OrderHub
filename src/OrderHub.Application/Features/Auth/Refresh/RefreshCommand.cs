using OrderHub.Application.Common.Messaging;
using OrderHub.Application.Common.Security;

namespace OrderHub.Application.Features.Auth.Refresh;

public record RefreshCommand(string Token)
    : ICommand<AuthResponse>;
