using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class Update : IEndpoint
{
    public sealed record UpdateCategoryRequest(
        string Name,
        string? Description,
        string? BackgroundColor,
        string? ForegroundColor);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("categories/{categoryId:guid}", async (
            Guid categoryId,
            UpdateCategoryRequest request,
            ICommandHandler<UpdateTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketCategoryCommand(
                categoryId,
                request.Name,
                request.Description,
                request.BackgroundColor,
                request.ForegroundColor);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .WithName("UpdateTicketCategory")
        .WithSummary("Rename or recolor an existing ticket category.")
        .HasPermission(Permissions.Categories.Manage)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "TicketCategories.NotFound", "No ticket category exists with the specified Id.")
        .ProducesError(StatusCodes.Status409Conflict, "TicketCategories.NameNotUnique", "A ticket category with the given name already exists in this scope.");
    }
}
