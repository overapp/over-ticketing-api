using SharedKernel;

namespace Domain.Users;

public sealed record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent;
