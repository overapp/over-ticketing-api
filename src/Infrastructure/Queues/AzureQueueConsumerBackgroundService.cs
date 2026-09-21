using System.Text;
using System.Text.Json;
using Application.Abstractions.Notifications;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Infrastructure.Queues;

internal sealed class AzureQueueConsumerBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AzureQueueStorageOptions> queueOptions,
    ILogger<AzureQueueConsumerBackgroundService> logger) : BackgroundService
{
    private QueueClient? _queueClient;
    private QueueClient? _poisonQueueClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AzureQueueStorageOptions options = queueOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogInformation("AzureQueueStorage connection string is not configured. Azure Queue consumer background service will not run.");
            return;
        }

        try
        {
            _queueClient = new QueueClient(options.ConnectionString, options.QueueName);
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            _poisonQueueClient = new QueueClient(options.ConnectionString, options.PoisonQueueName);
            await _poisonQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Azure Queue clients for consumer.");
            return;
        }

        var pollingInterval = TimeSpan.FromSeconds(Math.Max(options.PollingIntervalSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hadMessages = false;

            try
            {
                QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: 10,
                    visibilityTimeout: TimeSpan.FromSeconds(30),
                    cancellationToken: stoppingToken);

                if (messages.Length > 0)
                {
                    hadMessages = true;

                    foreach (QueueMessage message in messages)
                    {
                        await ProcessQueueMessageAsync(message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while polling or processing messages from Azure Storage Queue.");
            }

            if (!hadMessages)
            {
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessQueueMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        if (message.DequeueCount > 5)
        {
            logger.LogError(
                "Message {MessageId} exceeded max dequeue count ({DequeueCount}). Moving to poison queue {PoisonQueue}.",
                message.MessageId,
                message.DequeueCount,
                _poisonQueueClient!.Name);

            await _poisonQueueClient.SendMessageAsync(message.Body.ToString(), cancellationToken);
            await _queueClient!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        try
        {
            string rawBody = message.Body.ToString();
            string json;
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(rawBody);
                json = Encoding.UTF8.GetString(decodedBytes);
            }
            catch (FormatException)
            {
                json = rawBody;
            }

            OutboxQueueItem? queueItem = JsonSerializer.Deserialize<OutboxQueueItem>(json);
            if (queueItem is null)
            {
                logger.LogWarning("Invalid queue item received; deleting message {MessageId}.", message.MessageId);
                await _queueClient!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                return;
            }

            Type? eventType = ResolveType(queueItem.EventType);
            if (eventType is null)
            {
                logger.LogWarning("Unable to resolve type {EventType} for message {MessageId}.", queueItem.EventType, message.MessageId);
                await _queueClient!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                return;
            }

            object? deserialized = JsonSerializer.Deserialize(queueItem.PayloadJson, eventType);
            if (deserialized is null)
            {
                logger.LogWarning("Failed to deserialize payload for type {EventType}.", queueItem.EventType);
                await _queueClient!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                return;
            }

            using IServiceScope scope = serviceScopeFactory.CreateScope();
            INotificationPublisher publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

            if (deserialized is INotification notification)
            {
                await publisher.PublishAsync(notification, cancellationToken);
            }
            else if (deserialized is IDomainEvent domainEvent)
            {
                IEnumerable<IDomainEventToNotificationMapper> mappers =
                    scope.ServiceProvider.GetServices<IDomainEventToNotificationMapper>();

                foreach (IDomainEventToNotificationMapper mapper in mappers)
                {
                    INotification? mapped = mapper.Map(domainEvent);
                    if (mapped is not null)
                    {
                        await publisher.PublishAsync(mapped, cancellationToken);
                    }
                }
            }

            await _queueClient!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error processing message {MessageId}. It will be retried (current dequeue count: {DequeueCount}).",
                message.MessageId,
                message.DequeueCount);
        }
    }

    private static Type? ResolveType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        // Try matching simple name
        string simpleName = typeName.Contains(',') ? typeName.Split(',')[0].Trim() : typeName;
        int lastDot = simpleName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            string shortName = simpleName[(lastDot + 1)..];
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == shortName);
                if (type is not null)
                {
                    return type;
                }
            }
        }

        return null;
    }
}
