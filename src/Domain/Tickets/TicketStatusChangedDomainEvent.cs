using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketStatusChangedDomainEvent(Guid TicketId, TicketStatus PreviousStatus, TicketStatus NewStatus) : IDomainEvent;
