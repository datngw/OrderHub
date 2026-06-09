namespace OrderHub.Application.Features.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
