using Application.Abstractions.Messaging;
using Domain.Tickets;

namespace Application.Tickets.Create;

public sealed record CreateTicketCommand(
    Guid ProjectId,
    string Title,
    string Message,
    TicketPriority Priority = TicketPriority.Medium,
    IReadOnlyCollection<FileUploadModel>? Attachments = null) : ICommand<Guid>;
