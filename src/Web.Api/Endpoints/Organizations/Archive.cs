using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Archive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Archive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("organizations/{id:guid}/archive", async (
            Guid id,
            ICommandHandler<ArchiveOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new ArchiveOrganizationCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .HasPermission(Permissions.Organizations.Archive);
    }
}
