using SharedKernel;

namespace Domain.Organizations;

public sealed record OrganizationDeletedDomainEvent(Guid OrganizationId) : IDomainEvent;
