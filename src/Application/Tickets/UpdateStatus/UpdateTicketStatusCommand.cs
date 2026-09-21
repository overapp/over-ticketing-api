using Application.Abstractions.Messaging;
using Domain.Tickets;

namespace Application.Tickets.UpdateStatus;

public sealed record UpdateTicketStatusCommand(Guid TicketId, TicketStatus Status) : ICommand;
