using Application.Abstractions.Messaging;

namespace Application.Tickets.Reply;

public sealed record ReplyTicketCommand(
    Guid TicketId,
    string Content,
    bool IsInternal = false,
    IReadOnlyCollection<FileUploadModel>? Attachments = null) : ICommand<Guid>;
