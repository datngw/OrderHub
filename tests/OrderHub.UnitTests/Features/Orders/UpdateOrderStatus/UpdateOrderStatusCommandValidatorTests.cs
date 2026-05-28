using FluentValidation.TestHelper;
using OrderHub.Application.Features.Orders.UpdateOrderStatus;
using OrderHub.Domain.Orders;
using Xunit;

namespace OrderHub.UnitTests.Features.Orders.UpdateOrderStatus;

public class UpdateOrderStatusCommandValidatorTests
{
    private readonly UpdateOrderStatusCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithConfirmedStatus_ShouldHaveNoErrors()
    {
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatusEnum.Confirmed);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithShippedStatus_ShouldHaveNoErrors()
    {
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatusEnum.Shipped);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithDeliveredStatus_ShouldHaveNoErrors()
    {
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatusEnum.Delivered);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyOrderId_ShouldHaveError()
    {
        var command = new UpdateOrderStatusCommand(Guid.Empty, OrderStatusEnum.Confirmed);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public async Task Validate_WithPendingStatus_ShouldHaveError()
    {
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatusEnum.Pending);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.NewStatus);
    }

    [Fact]
    public async Task Validate_WithCancelledStatus_ShouldHaveError()
    {
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatusEnum.Cancelled);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.NewStatus);
    }
}
