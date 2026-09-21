using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCategoryUnarchivedDomainEvent(Guid CategoryId) : IDomainEvent;
