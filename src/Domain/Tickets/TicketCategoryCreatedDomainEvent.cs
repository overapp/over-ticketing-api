using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCategoryCreatedDomainEvent(Guid CategoryId) : IDomainEvent;
