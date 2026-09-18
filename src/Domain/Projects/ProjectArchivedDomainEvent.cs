using SharedKernel;

namespace Domain.Projects;

public sealed record ProjectArchivedDomainEvent(Guid ProjectId) : IDomainEvent;
