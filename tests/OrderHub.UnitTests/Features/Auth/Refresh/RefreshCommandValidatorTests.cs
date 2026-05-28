using FluentValidation.TestHelper;
using OrderHub.Application.Features.Auth.Refresh;
using Xunit;

namespace OrderHub.UnitTests.Features.Auth.Refresh;

public class RefreshCommandValidatorTests
{
    private readonly RefreshCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new RefreshCommand("some-refresh-token");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyToken_ShouldHaveError(string token)
    {
        var command = new RefreshCommand(token);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
