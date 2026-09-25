using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Unarchive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Unarchive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("projects/{id:guid}/unarchive", async (
            Guid id,
            ICommandHandler<UnarchiveProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new UnarchiveProjectCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("UnarchiveProject")
        .WithSummary("Reactivate an archived project.")
        .HasPermission(Permissions.Projects.Archive)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Projects.AlreadyActive", "The project is already active.");
    }
}
