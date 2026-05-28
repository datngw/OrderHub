using FluentValidation.TestHelper;
using OrderHub.Application.Features.Products.CreateProduct;
using Xunit;

namespace OrderHub.UnitTests.Features.Products.CreateProduct;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldHaveNoErrors()
    {
        var command = new CreateProductCommand(
            "SKU-001",
            "Widget",
            "A useful widget",
            9.99m,
            100,
            "Electronics");

        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptySKU_ShouldHaveError(string sku)
    {
        var command = new CreateProductCommand(sku, "Widget", "A useful widget", 9.99m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.SKU);
    }

    [Fact]
    public async Task Validate_WithTooLongSKU_ShouldHaveError()
    {
        var command = new CreateProductCommand(new string('a', 51), "Widget", "A useful widget", 9.99m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.SKU);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_ShouldHaveError(string name)
    {
        var command = new CreateProductCommand("SKU-001", name, "A useful widget", 9.99m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithTooLongName_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", new string('a', 201), "A useful widget", 9.99m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithTooLongDescription_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", "Widget", new string('a', 2001), 9.99m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Validate_WithZeroPrice_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", "Widget", "A useful widget", 0m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task Validate_WithNegativePrice_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", "Widget", "A useful widget", -1m, 100, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task Validate_WithNegativeStock_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", "Widget", "A useful widget", 9.99m, -1, "Electronics");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Stock);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyCategory_ShouldHaveError(string category)
    {
        var command = new CreateProductCommand("SKU-001", "Widget", "A useful widget", 9.99m, 100, category);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public async Task Validate_WithTooLongCategory_ShouldHaveError()
    {
        var command = new CreateProductCommand("SKU-001", "Widget", "A useful widget", 9.99m, 100, new string('a', 101));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}
