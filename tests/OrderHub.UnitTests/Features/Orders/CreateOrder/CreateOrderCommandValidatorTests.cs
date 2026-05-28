using FluentValidation.TestHelper;
using OrderHub.Application.Features.Orders.CreateOrder;
using Xunit;

namespace OrderHub.UnitTests.Features.Orders.CreateOrder;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(Guid.NewGuid(), 2),
            new CreateOrderItem(Guid.NewGuid(), 1)
        ]);

        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyItemsList_ShouldHaveError()
    {
        var command = new CreateOrderCommand([]);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldHaveError()
    {
        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(Guid.Empty, 2)
        ]);

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WithInvalidQuantity_ShouldHaveError(int quantity)
    {
        var command = new CreateOrderCommand(
        [
            new CreateOrderItem(Guid.NewGuid(), quantity)
        ]);

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }
}
