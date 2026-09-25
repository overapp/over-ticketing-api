using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.Unarchive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class Unarchive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("categories/{categoryId:guid}/unarchive", async (
            Guid categoryId,
            ICommandHandler<UnarchiveTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UnarchiveTicketCategoryCommand(categoryId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .WithName("UnarchiveTicketCategory")
        .WithSummary("Reactivate an archived ticket category.")
        .HasPermission(Permissions.Categories.Manage)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "TicketCategories.NotFound", "No ticket category exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "TicketCategories.AlreadyActive", "The ticket category is already active.");
    }
}
