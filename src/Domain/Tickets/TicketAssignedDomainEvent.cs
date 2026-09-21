using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketAssignedDomainEvent(Guid TicketId, Guid? AssignedToUserId) : IDomainEvent;
