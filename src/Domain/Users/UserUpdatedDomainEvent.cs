using SharedKernel;

namespace Domain.Users;

public sealed record UserUpdatedDomainEvent(Guid UserId) : IDomainEvent;
