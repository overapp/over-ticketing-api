using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Organizations.Get;
using Domain.Organizations;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Get : IEndpoint
{
    public sealed record Request(
        int Page = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        OrganizationStatus? Status = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizations", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationsQuery, PagedResponse<OrganizationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationsQuery(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.Status);

            Result<PagedResponse<OrganizationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .HasPermission(Permissions.Organizations.Read);
    }
}
