using FluentValidation;

namespace Application.TicketCategories.Unarchive;

internal sealed class UnarchiveTicketCategoryCommandValidator : AbstractValidator<UnarchiveTicketCategoryCommand>
{
    public UnarchiveTicketCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
            .NotEmpty();
    }
}
