using Application.Abstractions.Messaging;
using Domain.Tickets;

namespace Application.Tickets.UpdatePriority;

public sealed record UpdateTicketPriorityCommand(Guid TicketId, TicketPriority Priority) : ICommand;
