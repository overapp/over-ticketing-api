using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Unarchive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Unarchive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("organizations/{id:guid}/unarchive", async (
            Guid id,
            ICommandHandler<UnarchiveOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new UnarchiveOrganizationCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .HasPermission(Permissions.Organizations.Archive);
    }
}
