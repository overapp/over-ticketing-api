using FluentValidation;

namespace Application.Tickets.UpdateCategory;

internal sealed class UpdateTicketCategoryCommandValidator : AbstractValidator<UpdateTicketCategoryCommand>
{
    public UpdateTicketCategoryCommandValidator()
    {
        RuleFor(command => command.TicketId)
            .NotEmpty();

        RuleFor(command => command.CategoryId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("CategoryId must not be empty.");
    }
}
