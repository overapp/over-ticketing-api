using Application.Abstractions.Messaging;

namespace Application.Tickets.Assign;

public sealed record AssignTicketCommand(Guid TicketId, Guid? AssignedToUserId) : ICommand;
