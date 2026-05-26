using FluentValidation;

namespace OrderHub.Application.Features.Auth.Refresh;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Refresh token is required.");
    }
}
