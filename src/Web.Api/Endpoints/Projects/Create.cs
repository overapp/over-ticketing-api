using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Create : IEndpoint
{
    public sealed record CreateProjectRequest(Guid OrganizationId, string Name, string Description);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("projects", async (
            CreateProjectRequest request,
            ICommandHandler<CreateProjectCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectCommand(request.OrganizationId, request.Name, request.Description);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("CreateProject")
        .WithSummary("Create a new project under an organization.")
        .HasPermission(Permissions.Projects.Create)
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Organizations.NotFound", "No organization exists with the specified Id.");
    }
}
