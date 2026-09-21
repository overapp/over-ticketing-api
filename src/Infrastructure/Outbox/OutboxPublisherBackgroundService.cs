using System.Text.Json;
using Azure.Storage.Queues;
using Infrastructure.Database;
using Infrastructure.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

internal sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AzureQueueStorageOptions> queueOptions,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private QueueClient? _queueClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AzureQueueStorageOptions options = queueOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogInformation("AzureQueueStorage connection string is not configured. Outbox publisher background service will not run.");
            return;
        }

        try
        {
            _queueClient = new QueueClient(options.ConnectionString, options.QueueName);
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Azure Queue client for Outbox publisher.");
            return;
        }

        var pollingInterval = TimeSpan.FromSeconds(Math.Max(options.PollingIntervalSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingOutboxMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while publishing outbox messages.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task PublishPendingOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        DateTime utcNow = DateTime.UtcNow;
        int batchSize = queueOptions.Value.BatchSize;

        List<OutboxMessage> messages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && (m.LockedUntilUtc == null || m.LockedUntilUtc < utcNow))
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage message in messages)
        {
            message.LockedUntilUtc = utcNow.AddSeconds(30);
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (OutboxMessage message in messages)
        {
            try
            {
                var queueItem = new OutboxQueueItem(message.Id, message.Type, message.Content);
                string jsonPayload = JsonSerializer.Serialize(queueItem);

                string base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
                await _queueClient!.SendMessageAsync(base64Payload, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send outbox message {MessageId} to queue.", message.Id);
                message.RetryCount++;
                message.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                message.LockedUntilUtc = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, message.RetryCount) * 5));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
