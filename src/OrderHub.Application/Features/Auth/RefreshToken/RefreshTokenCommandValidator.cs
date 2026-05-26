using FluentValidation;

namespace OrderHub.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Refresh token is required.");
    }
}
