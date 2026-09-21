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

        RuleForEach(c => c.Roles)
            .Must(role => RoleNames.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage(role => $"The role '{role}' is invalid.");
    }
}
