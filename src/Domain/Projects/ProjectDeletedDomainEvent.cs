using SharedKernel;

namespace Domain.Projects;

public sealed record ProjectDeletedDomainEvent(Guid ProjectId) : IDomainEvent;
