using Microsoft.Extensions.Options;

namespace OrderHub.Application.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Key))
            return ValidateOptionsResult.Fail("Jwt:Key is required. Set it via environment variable or user secrets.");

        if (options.Key.Length < 32)
            return ValidateOptionsResult.Fail("Jwt:Key must be at least 32 characters long.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            return ValidateOptionsResult.Fail("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            return ValidateOptionsResult.Fail("Jwt:Audience is required.");

        return ValidateOptionsResult.Success;
    }
}
