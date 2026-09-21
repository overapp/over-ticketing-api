namespace Infrastructure.Queues;

public sealed record OutboxQueueItem(
    Guid OutboxMessageId,
    string EventType,
    string PayloadJson);
