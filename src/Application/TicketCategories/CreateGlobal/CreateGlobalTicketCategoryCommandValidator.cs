using Application.Common.Validation;
using FluentValidation;

namespace Application.TicketCategories.CreateGlobal;

internal sealed class CreateGlobalTicketCategoryCommandValidator : AbstractValidator<CreateGlobalTicketCategoryCommand>
{
    public CreateGlobalTicketCategoryCommandValidator()
    {
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
