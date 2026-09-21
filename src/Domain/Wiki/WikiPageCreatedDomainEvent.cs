using SharedKernel;

namespace Domain.Wiki;

public sealed record WikiPageCreatedDomainEvent(Guid WikiPageId) : IDomainEvent;
