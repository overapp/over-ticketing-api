using Application.Common.Validation;
using FluentValidation;

namespace Application.TicketCategories.Update;

internal sealed class UpdateTicketCategoryCommandValidator : AbstractValidator<UpdateTicketCategoryCommand>
{
    public UpdateTicketCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
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
