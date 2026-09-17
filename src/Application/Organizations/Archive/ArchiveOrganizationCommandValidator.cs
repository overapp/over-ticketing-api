using FluentValidation;

namespace Application.Organizations.Archive;

public sealed class ArchiveOrganizationCommandValidator : AbstractValidator<ArchiveOrganizationCommand>
{
    public ArchiveOrganizationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
    }
}
