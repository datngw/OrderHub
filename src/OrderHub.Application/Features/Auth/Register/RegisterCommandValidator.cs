using FluentValidation;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MinimumLength(UserConstraints.PasswordMinLength).WithMessage($"Password must be at least {UserConstraints.PasswordMinLength} characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.").MaximumLength(UserConstraints.FullNameMaxLength).WithMessage($"Full name must not exceed {UserConstraints.FullNameMaxLength} characters.");
    }
}
