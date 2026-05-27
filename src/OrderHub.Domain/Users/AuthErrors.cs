using OrderHub.Domain.Common;

namespace OrderHub.Domain.Users;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid credentials.", ErrorType.Unauthorized);

    public static readonly Error EmailAlreadyExists =
        new("Auth.EmailAlreadyExists", "A user with this email already exists.", ErrorType.Conflict);

    public static readonly Error InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "Invalid refresh token.", ErrorType.Unauthorized);

    public static readonly Error RefreshTokenRevoked =
        new("Auth.RefreshTokenRevoked", "Refresh token has been revoked.", ErrorType.Unauthorized);

    public static readonly Error RefreshTokenExpired =
        new("Auth.RefreshTokenExpired", "Refresh token has expired.", ErrorType.Unauthorized);
}
