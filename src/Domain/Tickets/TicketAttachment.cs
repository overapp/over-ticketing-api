using SharedKernel;

namespace Domain.Tickets;

public sealed class TicketAttachment : Entity
{
    public Guid Id { get; set; }

    public Guid TicketMessageId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
