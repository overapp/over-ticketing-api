using SharedKernel;

namespace Domain.Wiki;

public sealed record WikiPageDeletedDomainEvent(Guid WikiPageId) : IDomainEvent;
