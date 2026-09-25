using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdateCategory;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdateCategory : IEndpoint
{
    public sealed record UpdateTicketCategoryRequest(Guid? CategoryId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/category", async (
            Guid ticketId,
            UpdateTicketCategoryRequest request,
            ICommandHandler<UpdateTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketCategoryCommand(ticketId, request.CategoryId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("SetTicketCategory")
        .WithSummary("Set or clear the category assigned to a ticket.")
        .HasPermission(Permissions.Tickets.CategoryUpdate)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "No ticket exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.Closed", "The ticket is closed and cannot be modified.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "The caller does not have the Support or Admin role in this project.")
        .ProducesError(StatusCodes.Status404NotFound, "TicketCategories.NotFound", "No ticket category exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "TicketCategories.CategoryArchived", "The ticket category is archived and cannot be assigned.")
        .ProducesError(StatusCodes.Status400BadRequest, "TicketCategories.InvalidForProject", "The ticket category does not belong to the ticket's project.");
    }
}
