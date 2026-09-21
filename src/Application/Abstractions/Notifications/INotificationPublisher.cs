namespace Application.Abstractions.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
