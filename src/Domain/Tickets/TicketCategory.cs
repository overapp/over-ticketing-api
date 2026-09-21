using SharedKernel;

namespace Domain.Tickets;

public sealed class TicketCategory : Entity
{
    public Guid Id { get; set; }

    public Guid? ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string BackgroundColor { get; set; } = "#64748B";

    public string ForegroundColor { get; set; } = "#FFFFFF";

    public TicketCategoryStatus Status { get; set; } = TicketCategoryStatus.Active;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
