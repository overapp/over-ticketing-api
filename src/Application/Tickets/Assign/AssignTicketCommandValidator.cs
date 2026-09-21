using FluentValidation;

namespace Application.Tickets.Assign;

public sealed class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
