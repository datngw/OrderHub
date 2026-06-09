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
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(UserConstraints.PhoneMaxLength)
            .WithMessage($"Phone must not exceed {UserConstraints.PhoneMaxLength} characters.")
            .Matches(@"^(\+84|84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$")
            .WithMessage("Phone number must be a valid Vietnamese phone number.");
    }
}
