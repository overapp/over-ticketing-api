using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketMessageAddedDomainEvent(Guid TicketId, Guid MessageId, bool IsInternal) : IDomainEvent;
