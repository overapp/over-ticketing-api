using SharedKernel;

namespace Domain.Organizations;

public sealed record OrganizationArchivedDomainEvent(Guid OrganizationId) : IDomainEvent;
