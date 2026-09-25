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
        .WithName("UnarchiveOrganization")
        .WithSummary("Reactivate an archived organization.")
        .HasPermission(Permissions.Organizations.Archive)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Organizations.NotFound", "No organization exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Organizations.AlreadyActive", "The organization is already active.");
    }
}
