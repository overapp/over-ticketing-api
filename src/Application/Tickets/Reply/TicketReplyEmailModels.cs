namespace Application.Tickets.Reply;

public sealed record TicketReplyCustomerEmailModel(
    string RecipientName,
    Guid TicketId,
    string Title,
    string ProjectName,
    string RepliedByName,
    string MessageContent,
    Uri TicketUrl,
    string RepliedAt);

public sealed record TicketReplyStaffEmailModel(
    string RecipientName,
    Guid TicketId,
    string Title,
    string ProjectName,
    string AuthorName,
    string AuthorEmail,
    string MessageContent,
    Uri TicketUrl,
    string RepliedAt,
    bool IsAdmin);
