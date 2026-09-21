using Application.Common.Validation;
using FluentValidation;

namespace Application.TicketCategories.CreateProject;

internal sealed class CreateProjectTicketCategoryCommandValidator : AbstractValidator<CreateProjectTicketCategoryCommand>
{
    public CreateProjectTicketCategoryCommandValidator()
    {
        RuleFor(c => c.ProjectId)
            .NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.Description)
            .MaximumLength(500);

        RuleFor(c => c.BackgroundColor)
            .ValidCssHexColor();

        RuleFor(c => c.ForegroundColor)
            .ValidCssHexColor();
    }
}
