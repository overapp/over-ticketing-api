using SharedKernel;

namespace Domain.Tickets;

public sealed class TicketMessage : Entity
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<TicketAttachment> Attachments { get; set; } = [];
}
