using Application.Abstractions.Notifications;

namespace Application.Tickets.Create;

public sealed record TicketCreatedNotification(Guid TicketId) : INotification;
