using Application.TicketCategories;
using Domain.Tickets;

namespace Application.Tickets.GetById;

public sealed record TicketDetailResponse
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public Guid CreatedByUserId { get; init; }

    public Guid? AssignedToUserId { get; init; }

    public TicketCategoryResponse? Category { get; init; }

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

    public IReadOnlyCollection<TicketMessageResponse> Messages { get; init; } = [];
}

public sealed record TicketMessageResponse
{
    public Guid Id { get; init; }

    public Guid AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public bool IsInternal { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyCollection<TicketAttachmentResponse> Attachments { get; init; } = [];
}

public sealed record TicketAttachmentResponse
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public DateTime CreatedAt { get; init; }
}
