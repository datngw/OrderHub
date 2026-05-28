using FluentValidation.TestHelper;
using OrderHub.Application.Features.Auth.Register;
using Xunit;

namespace OrderHub.UnitTests.Features.Auth.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new RegisterCommand(
            "user@example.com",
            "Password1!",
            "John Doe");

        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyEmail_ShouldHaveError(string email)
    {
        var command = new RegisterCommand(email, "Password1!", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WithInvalidEmailFormat_ShouldHaveError()
    {
        var command = new RegisterCommand("not-an-email", "Password1!", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyPassword_ShouldHaveError(string password)
    {
        var command = new RegisterCommand("user@example.com", password, "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WithTooShortPassword_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "Pass1!a", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WithNoUppercasePassword_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "password1!", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WithNoLowercasePassword_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "PASSWORD1!", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WithNoDigitPassword_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "Password!!", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WithNoSpecialCharPassword_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "Password12", "John Doe");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyFullName_ShouldHaveError(string fullName)
    {
        var command = new RegisterCommand("user@example.com", "Password1!", fullName);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public async Task Validate_WithTooLongFullName_ShouldHaveError()
    {
        var command = new RegisterCommand("user@example.com", "Password1!", new string('a', 201));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
