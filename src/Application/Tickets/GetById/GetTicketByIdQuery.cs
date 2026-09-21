using Application.Abstractions.Messaging;

namespace Application.Tickets.GetById;

public sealed record GetTicketByIdQuery(Guid TicketId) : IQuery<TicketDetailResponse>;
