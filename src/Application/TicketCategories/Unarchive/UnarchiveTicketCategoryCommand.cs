using Application.Abstractions.Messaging;

namespace Application.TicketCategories.Unarchive;

public sealed record UnarchiveTicketCategoryCommand(Guid CategoryId) : ICommand;
