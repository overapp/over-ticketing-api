using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tickets/{ticketId:guid}", async (
            Guid ticketId,
            IQueryHandler<GetTicketByIdQuery, TicketDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTicketByIdQuery(ticketId);

            Result<TicketDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.Read);
    }
}
