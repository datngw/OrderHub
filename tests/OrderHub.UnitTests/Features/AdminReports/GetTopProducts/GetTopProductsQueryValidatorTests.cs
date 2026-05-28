using FluentValidation.TestHelper;
using OrderHub.Application.Features.AdminReports.GetTopProducts;
using Xunit;

namespace OrderHub.UnitTests.Features.AdminReports.GetTopProducts;

public class GetTopProductsQueryValidatorTests
{
    private readonly GetTopProductsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithDefaultValues_ShouldHaveNoErrors()
    {
        var query = new GetTopProductsQuery();
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithTopEqualToOne_ShouldHaveNoErrors()
    {
        var query = new GetTopProductsQuery(Top: 1);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithTopEqualToHundred_ShouldHaveNoErrors()
    {
        var query = new GetTopProductsQuery(Top: 100);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task Validate_WithInvalidTop_ShouldHaveError(int top)
    {
        var query = new GetTopProductsQuery(Top: top);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(x => x.Top);
    }
}
