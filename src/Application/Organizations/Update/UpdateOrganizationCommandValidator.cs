using FluentValidation;

namespace Application.Organizations.Update;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Logo)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
