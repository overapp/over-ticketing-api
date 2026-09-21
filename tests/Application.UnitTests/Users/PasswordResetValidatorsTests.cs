using Application.Users.ForgotPassword;
using Application.Users.ResetPassword;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class PasswordResetValidatorsTests
{
    private readonly ForgotPasswordCommandValidator _forgotValidator = new();
    private readonly ResetPasswordCommandValidator _resetValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    public void ForgotPasswordValidator_Should_HaveError_WhenEmailIsInvalid(string email)
    {
        var command = new ForgotPasswordCommand(email);
        TestValidationResult<ForgotPasswordCommand> result = _forgotValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void ForgotPasswordValidator_Should_NotHaveError_WhenEmailIsValid()
    {
        var command = new ForgotPasswordCommand("valid@example.com");
        TestValidationResult<ForgotPasswordCommand> result = _forgotValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void ResetPasswordValidator_Should_HaveError_WhenEmailIsInvalid(string email)
    {
        var command = new ResetPasswordCommand(email, "valid-token", "Valid123!", "Valid123!");
        TestValidationResult<ResetPasswordCommand> result = _resetValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResetPasswordValidator_Should_HaveError_WhenTokenIsEmpty(string token)
    {
        var command = new ResetPasswordCommand("valid@example.com", token, "Valid123!", "Valid123!");
        TestValidationResult<ResetPasswordCommand> result = _resetValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Token);
    }

    [Theory]
    [InlineData("short")] // < 6 chars
    [InlineData("nodigits!A")] // no digit
    [InlineData("NOLOWER123!")] // no lower
    [InlineData("noupper123!")] // no upper
    [InlineData("NoSpecial123")] // no non-alphanumeric
    public void ResetPasswordValidator_Should_HaveError_WhenPasswordIsWeak(string password)
    {
        var command = new ResetPasswordCommand("valid@example.com", "token", password, password);
        TestValidationResult<ResetPasswordCommand> result = _resetValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void ResetPasswordValidator_Should_HaveError_WhenPasswordsDoNotMatch()
    {
        var command = new ResetPasswordCommand("valid@example.com", "token", "Valid123!", "Different123!");
        TestValidationResult<ResetPasswordCommand> result = _resetValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ConfirmPassword);
    }

    [Fact]
    public void ResetPasswordValidator_Should_NotHaveError_WhenCommandIsValid()
    {
        var command = new ResetPasswordCommand("valid@example.com", "token", "Valid123!", "Valid123!");
        TestValidationResult<ResetPasswordCommand> result = _resetValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
