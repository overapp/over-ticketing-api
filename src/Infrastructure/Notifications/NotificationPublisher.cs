using System.Collections.Concurrent;
using Application.Abstractions.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Notifications;

internal sealed class NotificationPublisher(IServiceProvider serviceProvider) : INotificationPublisher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        Type notificationType = notification.GetType();
        Type handlerType = HandlerTypeDictionary.GetOrAdd(
            notificationType,
            nt => typeof(INotificationEventHandler<>).MakeGenericType(nt));

        IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);

        foreach (object? handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var handlerWrapper = HandlerWrapper.Create(handler, notificationType);
            await handlerWrapper.Handle(notification, cancellationToken);
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract Task Handle(INotification notification, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type notificationType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                notificationType,
                nt => typeof(HandlerWrapper<>).MakeGenericType(nt));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler)!;
        }
    }

    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : INotification
    {
        private readonly INotificationEventHandler<T> _handler = (INotificationEventHandler<T>)handler;

        public override async Task Handle(INotification notification, CancellationToken cancellationToken)
        {
            await _handler.Handle((T)notification, cancellationToken);
        }
    }
}
