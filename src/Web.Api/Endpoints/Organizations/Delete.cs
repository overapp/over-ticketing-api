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
        .WithName("DeleteOrganization")
        .WithSummary("Permanently delete an organization.")
        .HasPermission(Permissions.Organizations.Delete)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Organizations.NotFound", "No organization exists with the specified Id.");
    }
}
