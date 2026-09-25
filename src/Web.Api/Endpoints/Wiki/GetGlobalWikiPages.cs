using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Wiki.GetGlobalWikiPages;
using Domain.Wiki;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class GetGlobalWikiPages : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("wiki/global", async (
            int page,
            int pageSize,
            string? searchTerm,
            WikiPageStatus? status,
            Guid? parentPageId,
            IQueryHandler<GetGlobalWikiPagesQuery, PagedResponse<GlobalWikiPageSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetGlobalWikiPagesQuery(
                page == 0 ? 1 : page,
                pageSize == 0 ? 10 : pageSize,
                searchTerm,
                status,
                parentPageId);

            Result<PagedResponse<GlobalWikiPageSummaryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .WithName("GetGlobalWikiPages")
        .WithSummary("List global wiki pages.")
        .HasPermission(Permissions.Wiki.Read)
        .Produces<PagedResponse<GlobalWikiPageSummaryResponse>>(StatusCodes.Status200OK);
    }
}
