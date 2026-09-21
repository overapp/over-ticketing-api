namespace Infrastructure.Storage;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "ticket-attachments";
}
