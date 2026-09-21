using SharedKernel;

namespace Domain.Wiki;

public sealed record WikiPageArchivedDomainEvent(Guid WikiPageId) : IDomainEvent;
