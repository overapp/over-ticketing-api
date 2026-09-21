using Application.Abstractions.Messaging;

namespace Application.Tickets.UpdateCategory;

public sealed record UpdateTicketCategoryCommand(
    Guid TicketId,
    Guid? CategoryId) : ICommand;
