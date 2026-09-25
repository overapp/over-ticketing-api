using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Projects;
using Application.Projects.Get;
using Domain.Projects;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Get : IEndpoint
{
    public sealed record GetProjectsRequest(
        int Page = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        Guid? OrganizationId = null,
        ProjectStatus? Status = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects", async (
            [AsParameters] GetProjectsRequest request,
            IQueryHandler<GetProjectsQuery, PagedResponse<ProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectsQuery(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.OrganizationId,
                request.Status);

            Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .WithName("GetProjects")
        .WithSummary("List projects, with optional search and status filters.")
        .HasPermission(Permissions.Projects.Read)
        .Produces<PagedResponse<ProjectResponse>>(StatusCodes.Status200OK);
    }
}
