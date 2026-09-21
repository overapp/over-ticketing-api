using Domain.Users;
using FluentValidation;

namespace Application.Users.Create;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[0-9]").WithMessage("'Password' must contain at least one digit.")
            .Matches("[a-z]").WithMessage("'Password' must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("'Password' must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("'Password' must contain at least one non-alphanumeric character.");

        RuleForEach(c => c.Roles)
            .Must(role => RoleNames.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage(role => $"The role '{role}' is invalid.");
    }
}
