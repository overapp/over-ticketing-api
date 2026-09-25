using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations;
using Application.Organizations.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizations/{id:guid}", async (
            Guid id,
            IQueryHandler<GetOrganizationByIdQuery, OrganizationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationByIdQuery(id);

            Result<OrganizationResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .WithName("GetOrganizationById")
        .WithSummary("Retrieve an organization by its Id.")
        .HasPermission(Permissions.Organizations.Read)
        .Produces<OrganizationResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Organizations.NotFound", "No organization exists with the specified Id.");
    }
}
