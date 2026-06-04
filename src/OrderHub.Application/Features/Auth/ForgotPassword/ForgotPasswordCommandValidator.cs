using FluentValidation;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Reset code is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(UserConstraints.PasswordMinLength)
                .WithMessage($"Password must be at least {UserConstraints.PasswordMinLength} characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}
