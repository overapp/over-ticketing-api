using FluentValidation;

namespace Application.Users.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(c => c.Token)
            .NotEmpty();

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[0-9]").WithMessage("'New Password' must contain at least one digit.")
            .Matches("[a-z]").WithMessage("'New Password' must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("'New Password' must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("'New Password' must contain at least one non-alphanumeric character.");

        RuleFor(c => c.ConfirmPassword)
            .Equal(c => c.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}
