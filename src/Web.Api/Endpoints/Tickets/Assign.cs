using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.Assign;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class Assign : IEndpoint
{
    public sealed record AssignTicketRequest(Guid? AssignedToUserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/assign", async (
            Guid ticketId,
            AssignTicketRequest request,
            ICommandHandler<AssignTicketCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignTicketCommand(ticketId, request.AssignedToUserId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("AssignTicket")
        .WithSummary("Assign or unassign a ticket to a support user.")
        .HasPermission(Permissions.Tickets.Assign)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "No ticket exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.Closed", "The ticket is closed and cannot be modified.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller or the target assignee is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "The caller does not have the Support or Admin role in this project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.AssigneeNotSupport", "The target user does not have the Support role in this project.");
    }
}
