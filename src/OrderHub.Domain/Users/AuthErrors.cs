using OrderHub.Domain.Common;

namespace OrderHub.Domain.Users;

public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Problem("Auth.InvalidCredentials", "Invalid credentials.");

    public static Error EmailAlreadyExists =>
        Error.Conflict("Auth.EmailAlreadyExists", "A user with this email already exists.");

    public static Error InvalidRefreshToken =>
        Error.Problem("Auth.InvalidRefreshToken", "Invalid refresh token.");

    public static Error RefreshTokenRevoked =>
        Error.Problem("Auth.RefreshTokenRevoked", "Refresh token has been revoked.");

    public static Error RefreshTokenExpired =>
        Error.Problem("Auth.RefreshTokenExpired", "Refresh token has expired.");
}
