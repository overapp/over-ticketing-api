using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdateStatus;
using Domain.Tickets;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdateStatus : IEndpoint
{
    public sealed record UpdateTicketStatusRequest(TicketStatus Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/status", async (
            Guid ticketId,
            UpdateTicketStatusRequest request,
            ICommandHandler<UpdateTicketStatusCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketStatusCommand(ticketId, request.Status);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("UpdateTicketStatus")
        .WithSummary("Transition a ticket to a new status.")
        .HasPermission(Permissions.Tickets.StatusUpdate)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "No ticket exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.Closed", "The ticket is closed and cannot be modified.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "Standard users can only update their own tickets.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.InvalidStatusTransition", "Standard users may only transition their own ticket to Resolved or Closed.");
    }
}
