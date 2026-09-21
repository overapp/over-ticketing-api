using SharedKernel;

namespace Domain.Users;

public sealed record UserDeletedDomainEvent(Guid UserId) : IDomainEvent;
