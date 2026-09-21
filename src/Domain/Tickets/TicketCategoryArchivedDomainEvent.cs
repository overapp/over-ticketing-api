using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCategoryArchivedDomainEvent(Guid CategoryId) : IDomainEvent;
