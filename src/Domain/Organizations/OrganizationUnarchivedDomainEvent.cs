using SharedKernel;

namespace Domain.Organizations;

public sealed record OrganizationUnarchivedDomainEvent(Guid OrganizationId) : IDomainEvent;
