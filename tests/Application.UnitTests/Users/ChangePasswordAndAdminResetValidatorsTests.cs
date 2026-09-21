using Application.Users.AdminResetPassword;
using Application.Users.ChangePassword;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class ChangePasswordAndAdminResetValidatorsTests
{
    private readonly ChangePasswordCommandValidator _changePasswordValidator = new();
    private readonly AdminResetPasswordCommandValidator _adminResetValidator = new();

    [Fact]
    public void ChangePasswordValidator_Should_HaveError_WhenCurrentPasswordIsEmpty()
    {
        var command = new ChangePasswordCommand("", "NewPass123!", "NewPass123!");
        TestValidationResult<ChangePasswordCommand> result = _changePasswordValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.CurrentPassword);
    }

    [Theory]
    [InlineData("short")] // < 6 chars
    [InlineData("nodigits!A")] // no digit
    [InlineData("NOLOWER123!")] // no lower
    [InlineData("noupper123!")] // no upper
    [InlineData("NoSpecial123")] // no non-alphanumeric
    public void ChangePasswordValidator_Should_HaveError_WhenNewPasswordIsWeak(string password)
    {
        var command = new ChangePasswordCommand("OldPass123!", password, password);
        TestValidationResult<ChangePasswordCommand> result = _changePasswordValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void ChangePasswordValidator_Should_HaveError_WhenNewPasswordSameAsCurrent()
    {
        var command = new ChangePasswordCommand("SamePass123!", "SamePass123!", "SamePass123!");
        TestValidationResult<ChangePasswordCommand> result = _changePasswordValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void ChangePasswordValidator_Should_HaveError_WhenConfirmPasswordDoesNotMatch()
    {
        var command = new ChangePasswordCommand("OldPass123!", "NewPass123!", "Different123!");
        TestValidationResult<ChangePasswordCommand> result = _changePasswordValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ConfirmNewPassword);
    }

    [Fact]
    public void ChangePasswordValidator_Should_NotHaveError_WhenCommandIsValid()
    {
        var command = new ChangePasswordCommand("OldPass123!", "NewPass123!", "NewPass123!");
        TestValidationResult<ChangePasswordCommand> result = _changePasswordValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AdminResetValidator_Should_HaveError_WhenUserIdIsEmpty()
    {
        var command = new AdminResetPasswordCommand(Guid.Empty);
        TestValidationResult<AdminResetPasswordCommand> result = _adminResetValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void AdminResetValidator_Should_NotHaveError_WhenUserIdIsValid()
    {
        var command = new AdminResetPasswordCommand(Guid.NewGuid());
        TestValidationResult<AdminResetPasswordCommand> result = _adminResetValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
