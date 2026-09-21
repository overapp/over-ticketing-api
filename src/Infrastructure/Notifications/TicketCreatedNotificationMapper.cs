using Application.Abstractions.Notifications;
using Application.Tickets.Create;
using Domain.Tickets;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class TicketCreatedNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        TicketCreatedDomainEvent e => new TicketCreatedNotification(e.TicketId),
        _ => null
    };
}
