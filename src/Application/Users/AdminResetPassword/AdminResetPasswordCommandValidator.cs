using FluentValidation;

namespace Application.Users.AdminResetPassword;

internal sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
