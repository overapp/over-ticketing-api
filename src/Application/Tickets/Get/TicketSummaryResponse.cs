using Domain.Tickets;

namespace Application.Tickets.Get;

public sealed record TicketSummaryResponse
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public Guid CreatedByUserId { get; init; }

    public Guid? AssignedToUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public TicketStatus Status { get; init; }

    public TicketPriority Priority { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public DateTime? FirstResponseDueAt { get; init; }

    public DateTime? FirstResponseAt { get; init; }

    public DateTime? ResolutionDueAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public DateTime? ClosedAt { get; init; }
}
