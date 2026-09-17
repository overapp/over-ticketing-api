using FluentValidation;

namespace Application.Organizations.Delete;

public sealed class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
    }
}
