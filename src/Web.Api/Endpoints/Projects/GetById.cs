using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects;
using Application.Projects.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects/{id:guid}", async (
            Guid id,
            IQueryHandler<GetProjectByIdQuery, ProjectResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectByIdQuery(id);

            Result<ProjectResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("GetProjectById")
        .WithSummary("Retrieve a project by its Id.")
        .HasPermission(Permissions.Projects.Read)
        .Produces<ProjectResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.");
    }
}
