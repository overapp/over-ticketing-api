using Application.Abstractions.Notifications;
using Application.Users.Create;
using Domain.Users;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class UserCreatedNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserCreatedDomainEvent e => new UserCreatedNotification(e.UserId, e.TemporaryPassword),
        UserRegisteredDomainEvent e => new UserCreatedNotification(e.UserId),
        _ => null
    };
}
