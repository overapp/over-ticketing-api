using Application.Abstractions.Messaging;

namespace Application.TicketCategories.Update;

public sealed record UpdateTicketCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description = null,
    string? BackgroundColor = null,
    string? ForegroundColor = null) : ICommand;
