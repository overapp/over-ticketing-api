using FluentValidation;

namespace Application.Tickets.UpdatePriority;

public sealed class UpdateTicketPriorityCommandValidator : AbstractValidator<UpdateTicketPriorityCommand>
{
    public UpdateTicketPriorityCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Priority).IsInEnum();
    }
}
