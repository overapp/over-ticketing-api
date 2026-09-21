using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Tickets;

namespace Application.Tickets.Get;

public sealed record GetTicketsQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? AssignedToUserId = null,
    Guid? CategoryId = null) : IQuery<PagedResponse<TicketSummaryResponse>>;
