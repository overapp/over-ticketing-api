using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketPriorityChangedDomainEvent(Guid TicketId, TicketPriority PreviousPriority, TicketPriority NewPriority) : IDomainEvent;
