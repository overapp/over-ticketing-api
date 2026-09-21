using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Wiki.GetProjectWikiPages;
using Domain.Wiki;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class GetProjectWikiPages : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects/{projectId:guid}/wiki", async (
            Guid projectId,
            int page,
            int pageSize,
            string? searchTerm,
            WikiPageStatus? status,
            Guid? parentPageId,
            IQueryHandler<GetProjectWikiPagesQuery, PagedResponse<WikiPageSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectWikiPagesQuery(
                projectId,
                page == 0 ? 1 : page,
                pageSize == 0 ? 10 : pageSize,
                searchTerm,
                status,
                parentPageId);

            Result<PagedResponse<WikiPageSummaryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .HasPermission(Permissions.Wiki.Read);
    }
}
