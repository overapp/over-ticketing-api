using FluentValidation;

namespace Application.Organizations.Create;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Logo)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
