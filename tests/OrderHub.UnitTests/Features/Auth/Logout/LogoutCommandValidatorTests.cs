using FluentValidation.TestHelper;
using OrderHub.Application.Features.Auth.Logout;
using Xunit;

namespace OrderHub.UnitTests.Features.Auth.Logout;

public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new LogoutCommand("some-refresh-token");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyRefreshToken_ShouldHaveError(string refreshToken)
    {
        var command = new LogoutCommand(refreshToken);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
