using Domain.Tickets;

namespace Application.TicketCategories;

public sealed record TicketCategoryResponse(
    Guid Id,
    Guid? ProjectId,
    string Name,
    string Description,
    string BackgroundColor,
    string ForegroundColor,
    TicketCategoryStatus Status);
