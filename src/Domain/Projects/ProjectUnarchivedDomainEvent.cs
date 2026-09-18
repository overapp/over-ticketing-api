using SharedKernel;

namespace Domain.Projects;

public sealed record ProjectUnarchivedDomainEvent(Guid ProjectId) : IDomainEvent;
