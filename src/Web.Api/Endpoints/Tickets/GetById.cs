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
        .WithName("GetTicketById")
        .WithSummary("Retrieve a ticket with its full message history.")
        .HasPermission(Permissions.Tickets.Read)
        .Produces<TicketDetailResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "No ticket exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "Standard users can only view tickets they created.");
    }
}
