using Application.Abstractions.Notifications;
using Application.Users.ChangePassword;
using Domain.Users;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class UserPasswordChangedNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserPasswordChangedDomainEvent e => new UserPasswordChangedNotification(e.UserId),
        _ => null
    };
}
