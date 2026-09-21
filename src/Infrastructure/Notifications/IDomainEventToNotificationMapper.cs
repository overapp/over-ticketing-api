using Application.Abstractions.Notifications;
using SharedKernel;

namespace Infrastructure.Notifications;

public interface IDomainEventToNotificationMapper
{
    INotification? Map(IDomainEvent domainEvent);
}
