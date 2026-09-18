using SharedKernel;

namespace Domain.Projects;

public sealed record ProjectUpdatedDomainEvent(Guid ProjectId) : IDomainEvent;
