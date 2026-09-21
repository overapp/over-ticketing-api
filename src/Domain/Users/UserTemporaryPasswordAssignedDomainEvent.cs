using SharedKernel;

namespace Domain.Users;

public sealed record UserTemporaryPasswordAssignedDomainEvent(Guid UserId, string TemporaryPassword) : IDomainEvent;
