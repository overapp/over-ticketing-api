namespace Infrastructure.Queues;

public sealed class AzureQueueStorageOptions
{
    public const string SectionName = "AzureQueueStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "notifications-queue";

    public string PoisonQueueName { get; set; } = "notifications-poison";

    public int BatchSize { get; set; } = 20;

    public int PollingIntervalSeconds { get; set; } = 2;
}
