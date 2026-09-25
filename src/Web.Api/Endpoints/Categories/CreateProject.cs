using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.CreateProject;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class CreateProject : IEndpoint
{
    public sealed record CreateProjectCategoryRequest(
        string Name,
        string? Description,
        string? BackgroundColor,
        string? ForegroundColor);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("projects/{projectId:guid}/categories", async (
            Guid projectId,
            CreateProjectCategoryRequest request,
            ICommandHandler<CreateProjectTicketCategoryCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectTicketCategoryCommand(
                projectId,
                request.Name,
                request.Description,
                request.BackgroundColor,
                request.ForegroundColor);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"projects/{projectId}/categories/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .WithName("CreateProjectTicketCategory")
        .WithSummary("Create a ticket category scoped to a single project.")
        .HasPermission(Permissions.Categories.Manage)
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status409Conflict, "TicketCategories.NameNotUnique", "A ticket category with the given name already exists in this project.");
    }
}
