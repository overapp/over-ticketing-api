using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Update : IEndpoint
{
    public sealed record UpdateProjectRequest(string Name, string Description);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("projects/{id:guid}", async (
            Guid id,
            UpdateProjectRequest request,
            ICommandHandler<UpdateProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProjectCommand(id, request.Name, request.Description);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("UpdateProject")
        .WithSummary("Update a project's name and description.")
        .HasPermission(Permissions.Projects.Edit)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.");
    }
}
