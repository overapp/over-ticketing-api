using Application.TicketCategories.CreateGlobal;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class CreateGlobalTicketCategoryCommandValidatorTests
{
    private readonly CreateGlobalTicketCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_HaveValidationError_WhenNameIsEmpty()
    {
        var command = new CreateGlobalTicketCategoryCommand("");

        TestValidationResult<CreateGlobalTicketCategoryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_Should_HaveValidationError_WhenNameExceeds100Characters()
    {
        var command = new CreateGlobalTicketCategoryCommand(new string('a', 101));

        TestValidationResult<CreateGlobalTicketCategoryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_Should_HaveValidationError_WhenBackgroundColorIsInvalidHex()
    {
        var command = new CreateGlobalTicketCategoryCommand("Bug", BackgroundColor: "invalid-color");

        TestValidationResult<CreateGlobalTicketCategoryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BackgroundColor);
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    [InlineData("#FFFFFFFF")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_Should_NotHaveValidationError_WhenBackgroundColorIsValidHexOrEmpty(string? color)
    {
        var command = new CreateGlobalTicketCategoryCommand("Bug", BackgroundColor: color);

        TestValidationResult<CreateGlobalTicketCategoryCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.BackgroundColor);
    }

    [Fact]
    public void Validate_Should_HaveValidationError_WhenForegroundColorIsInvalidHex()
    {
        var command = new CreateGlobalTicketCategoryCommand("Bug", ForegroundColor: "#12345");

        TestValidationResult<CreateGlobalTicketCategoryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ForegroundColor);
    }
}
