using Application.Settings.Update;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests.Settings;

public sealed class UpdateUserSettingsCommandValidatorTests
{
    private readonly UpdateUserSettingsCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenEmailNotificationsIsNull()
    {
        var command = new UpdateUserSettingsCommand(null!);

        TestValidationResult<UpdateUserSettingsCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmailNotifications);
    }

    [Fact]
    public void Should_NotHaveError_WhenEmailNotificationsIsValid()
    {
        var command = new UpdateUserSettingsCommand(new UpdateEmailNotificationSettings(true, true));

        TestValidationResult<UpdateUserSettingsCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.EmailNotifications);
    }
}
