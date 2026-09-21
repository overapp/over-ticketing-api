using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCategoryUpdatedDomainEvent(Guid CategoryId) : IDomainEvent;
