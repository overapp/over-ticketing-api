using FluentValidation;

namespace Application.Organizations.Unarchive;

public sealed class UnarchiveOrganizationCommandValidator : AbstractValidator<UnarchiveOrganizationCommand>
{
    public UnarchiveOrganizationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
    }
}
