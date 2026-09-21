using SharedKernel;

namespace Domain.Wiki;

public sealed record WikiPageUpdatedDomainEvent(Guid WikiPageId) : IDomainEvent;
