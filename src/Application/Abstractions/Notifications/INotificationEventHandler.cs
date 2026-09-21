namespace Application.Abstractions.Notifications;

public interface INotificationEventHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
