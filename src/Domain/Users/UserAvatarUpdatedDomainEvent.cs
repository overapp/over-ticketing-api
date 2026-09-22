using SharedKernel;

namespace Domain.Users;

#pragma warning disable CA1054
public sealed record UserAvatarUpdatedDomainEvent(Guid UserId, string? OldProfilePictureUrl, string? NewProfilePictureUrl) : IDomainEvent;
#pragma warning restore CA1054
