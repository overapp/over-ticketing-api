using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdatePriority;
using Domain.Tickets;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdatePriority : IEndpoint
{
    public sealed record UpdateTicketPriorityRequest(TicketPriority Priority);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/priority", async (
            Guid ticketId,
            UpdateTicketPriorityRequest request,
            ICommandHandler<UpdateTicketPriorityCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketPriorityCommand(ticketId, request.Priority);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("UpdateTicketPriority")
        .WithSummary("Change a ticket's priority and recalculate its SLA due dates.")
        .HasPermission(Permissions.Tickets.PriorityUpdate)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "No ticket exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.Closed", "The ticket is closed and cannot be modified.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "The caller does not have the Support or Admin role in this project.");
    }
}
