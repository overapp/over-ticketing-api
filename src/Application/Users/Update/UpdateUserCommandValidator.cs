using Domain.Users;
using FluentValidation;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleForEach(c => c.Roles)
            .Must(role => RoleNames.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage(role => $"The role '{role}' is invalid.");
    }
}
