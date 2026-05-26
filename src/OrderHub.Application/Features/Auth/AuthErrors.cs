using OrderHub.Application.Common.Results;

namespace OrderHub.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new Error(
        "Auth.InvalidCredentials", "Invalid credentials.", 401);

    public static readonly Error EmailAlreadyExists = new Error(
        "Auth.EmailAlreadyExists", "A user with this email already exists.", 409);

    public static readonly Error InvalidRefreshToken = new Error(
        "Auth.InvalidRefreshToken", "Invalid refresh token.", 401);

    public static readonly Error RefreshTokenRevoked = new Error(
        "Auth.RefreshTokenRevoked", "Refresh token has been revoked.", 401);

    public static readonly Error RefreshTokenExpired = new Error(
        "Auth.RefreshTokenExpired", "Refresh token has expired.", 401);
}
