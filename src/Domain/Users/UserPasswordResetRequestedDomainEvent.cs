using SharedKernel;

namespace Domain.Users;

public sealed record UserPasswordResetRequestedDomainEvent(
    Guid UserId,
    string Email,
    string ResetToken) : IDomainEvent;
