using Application.Abstractions.Notifications;
using Application.Tickets.Reply;
using Domain.Tickets;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class TicketReplyNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        TicketMessageAddedDomainEvent e => new TicketMessageAddedNotification(e.TicketId, e.MessageId, e.IsInternal),
        _ => null
    };
}
