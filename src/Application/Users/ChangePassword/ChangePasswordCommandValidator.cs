using Domain.Users;
using FluentValidation;

namespace Application.Users.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty();

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[0-9]").WithMessage("'New Password' must contain at least one digit.")
            .Matches("[a-z]").WithMessage("'New Password' must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("'New Password' must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("'New Password' must contain at least one non-alphanumeric character.")
            .NotEqual(c => c.CurrentPassword).WithMessage(UserErrors.NewPasswordSameAsCurrent.Description);

        RuleFor(c => c.ConfirmNewPassword)
            .NotEmpty()
            .Equal(c => c.NewPassword).WithMessage("The new password and confirmation password do not match.");
    }
}
