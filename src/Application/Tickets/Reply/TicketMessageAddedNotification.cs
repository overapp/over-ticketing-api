using Application.Abstractions.Notifications;

namespace Application.Tickets.Reply;

public sealed record TicketMessageAddedNotification(
    Guid TicketId,
    Guid MessageId,
    bool IsInternal) : INotification;
