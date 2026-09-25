using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Archive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Archive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("projects/{id:guid}/archive", async (
            Guid id,
            ICommandHandler<ArchiveProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new ArchiveProjectCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("ArchiveProject")
        .WithSummary("Archive a project.")
        .HasPermission(Permissions.Projects.Archive)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Projects.AlreadyArchived", "The project is already archived.");
    }
}
