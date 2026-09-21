using Application.Abstractions.Messaging;

namespace Application.TicketCategories.CreateProject;

public sealed record CreateProjectTicketCategoryCommand(
    Guid ProjectId,
    string Name,
    string? Description = null,
    string? BackgroundColor = null,
    string? ForegroundColor = null) : ICommand<Guid>;
