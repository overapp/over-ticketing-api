using Application.Abstractions.Messaging;
using Domain.Tickets;

namespace Application.Tickets.Create;

public sealed record CreateTicketCommand(
    Guid ProjectId,
    string Title,
    string Message,
    TicketPriority Priority = TicketPriority.Medium,
    Guid? CategoryId = null,
    IReadOnlyCollection<FileUploadModel>? Attachments = null) : ICommand<Guid>;
