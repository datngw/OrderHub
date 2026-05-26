using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken)
    : ICommand;
