using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.GetProjectWikiPageBySlugOrId;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class GetProjectWikiPageBySlugOrId : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects/{projectId:guid}/wiki/{slugOrId}", async (
            Guid projectId,
            string slugOrId,
            IQueryHandler<GetProjectWikiPageBySlugOrIdQuery, ProjectWikiPageResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectWikiPageBySlugOrIdQuery(projectId, slugOrId);

            Result<ProjectWikiPageResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .WithName("GetProjectWikiPageBySlugOrId")
        .WithSummary("Retrieve a project wiki page by its slug or Id.")
        .HasPermission(Permissions.Wiki.Read)
        .Produces<ProjectWikiPageResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.UnauthorizedAccess", "The caller is not assigned to this project, or can only read published, non-internal pages.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFound", "No wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFoundBySlug", "No wiki page exists with the specified slug.");
    }
}
