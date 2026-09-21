using Application.Abstractions.Notifications;
using Application.Users.AdminResetPassword;
using Domain.Users;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class UserTemporaryPasswordAssignedNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserTemporaryPasswordAssignedDomainEvent e => new UserTemporaryPasswordAssignedNotification(
            e.UserId,
            e.TemporaryPassword),
        _ => null
    };
}
