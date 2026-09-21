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
        .HasPermission(Permissions.Wiki.Read);
    }
}
