using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.GetGlobalWikiPageBySlugOrId;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class GetGlobalWikiPageBySlugOrId : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("wiki/global/{slugOrId}", async (
            string slugOrId,
            IQueryHandler<GetGlobalWikiPageBySlugOrIdQuery, GlobalWikiPageResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetGlobalWikiPageBySlugOrIdQuery(slugOrId);

            Result<GlobalWikiPageResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .WithName("GetGlobalWikiPageBySlugOrId")
        .WithSummary("Retrieve a global wiki page by its slug or Id.")
        .HasPermission(Permissions.Wiki.Read)
        .Produces<GlobalWikiPageResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFound", "No wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFoundBySlug", "No wiki page exists with the specified slug.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.UnauthorizedAccess", "Standard users can only read published, non-internal pages.");
    }
}
