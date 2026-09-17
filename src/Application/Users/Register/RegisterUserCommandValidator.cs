using FluentValidation;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();

        // These rules mirror ASP.NET Core Identity's default IdentityOptions.Password, so
        // validation errors surface early, before UserManager.CreateAsync is even called.
        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[0-9]").WithMessage("'Password' must contain at least one digit.")
            .Matches("[a-z]").WithMessage("'Password' must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("'Password' must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("'Password' must contain at least one non-alphanumeric character.");
    }
}
