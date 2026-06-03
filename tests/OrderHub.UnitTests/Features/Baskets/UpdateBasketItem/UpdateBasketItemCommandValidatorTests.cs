using FluentValidation.TestHelper;
using OrderHub.Application.Features.Baskets.UpdateBasketItem;

namespace OrderHub.UnitTests.Features.Baskets.UpdateBasketItem;

public class UpdateBasketItemCommandValidatorTests
{
    private readonly UpdateBasketItemCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new UpdateBasketItemCommand(Guid.NewGuid(), 5);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldHaveError()
    {
        var command = new UpdateBasketItemCommand(Guid.Empty, 5);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task Validate_WithInvalidQuantity_ShouldHaveError(int quantity)
    {
        var command = new UpdateBasketItemCommand(Guid.NewGuid(), quantity);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}
