using FluentValidation;

namespace Application.TicketCategories.Archive;

internal sealed class ArchiveTicketCategoryCommandValidator : AbstractValidator<ArchiveTicketCategoryCommand>
{
    public ArchiveTicketCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
            .NotEmpty();
    }
}
