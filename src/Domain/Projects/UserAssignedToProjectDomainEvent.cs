using SharedKernel;

namespace Domain.Projects;

public sealed record UserAssignedToProjectDomainEvent(
    Guid AssignmentId,
    Guid ProjectId,
    Guid UserId,
    string Role) : IDomainEvent;
