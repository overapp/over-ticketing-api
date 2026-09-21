using Application.Abstractions.Messaging;

namespace Application.TicketCategories.Archive;

public sealed record ArchiveTicketCategoryCommand(Guid CategoryId) : ICommand;
