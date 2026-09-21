using Application.Abstractions.Messaging;

namespace Application.TicketCategories.CreateGlobal;

public sealed record CreateGlobalTicketCategoryCommand(
    string Name,
    string? Description = null,
    string? BackgroundColor = null,
    string? ForegroundColor = null) : ICommand<Guid>;
