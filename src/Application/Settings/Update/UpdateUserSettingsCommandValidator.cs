using FluentValidation;

namespace Application.Settings.Update;

public sealed class UpdateUserSettingsCommandValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    public UpdateUserSettingsCommandValidator()
    {
        RuleFor(c => c.EmailNotifications)
            .NotNull();
    }
}
