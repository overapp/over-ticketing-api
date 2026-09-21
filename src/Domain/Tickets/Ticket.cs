using Domain.Projects;
using Domain.Users;
using SharedKernel;

namespace Domain.Tickets;

public sealed class Ticket : Entity
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public Guid? CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? FirstResponseDueAt { get; set; }

    public DateTime? FirstResponseAt { get; set; }

    public DateTime? ResolutionDueAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<TicketMessage> Messages { get; set; } = [];
}
