using SharedKernel;

namespace Domain.Tickets;

public sealed record TicketCategoryChangedDomainEvent(
    Guid TicketId,
    Guid? PreviousCategoryId,
    Guid? NewCategoryId) : IDomainEvent;
