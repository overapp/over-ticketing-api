using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("projects/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new DeleteProjectCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("DeleteProject")
        .WithSummary("Permanently delete a project.")
        .HasPermission(Permissions.Projects.Delete)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.");
    }
}
