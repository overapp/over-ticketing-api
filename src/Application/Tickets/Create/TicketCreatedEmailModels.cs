namespace Application.Tickets.Create;

public sealed record TicketCreatedAuthorEmailModel(
    string RecipientName,
    Guid TicketId,
    string Title,
    string Priority,
    string Status,
    string ProjectName,
    Uri TicketUrl,
    string CreatedAt,
    string? FirstResponseDueAt);

public sealed record TicketCreatedSupportEmailModel(
    string RecipientName,
    Guid TicketId,
    string Title,
    string Priority,
    string ProjectName,
    string AuthorName,
    string AuthorEmail,
    string InitialMessage,
    Uri TicketUrl,
    string CreatedAt,
    string? FirstResponseDueAt,
    bool IsAdminFallback);
