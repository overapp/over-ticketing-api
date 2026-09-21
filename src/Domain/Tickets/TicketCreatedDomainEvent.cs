using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCreatedDomainEvent(Guid TicketId) : IDomainEvent;
