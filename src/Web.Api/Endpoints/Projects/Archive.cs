using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Archive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Archive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("projects/{id:guid}/archive", async (
            Guid id,
            ICommandHandler<ArchiveProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            Result result = await handler.Handle(new ArchiveProjectCommand(id), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .HasPermission(Permissions.Projects.Archive);
    }
}
