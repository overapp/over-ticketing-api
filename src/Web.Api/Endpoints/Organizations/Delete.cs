using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("organizations/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteOrganizationCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .HasPermission(Permissions.Organizations.Delete);
    }
}
