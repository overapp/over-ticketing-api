using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Update : IEndpoint
{
    public sealed record UpdateOrganizationRequest(string Name, string Logo);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("organizations/{id:guid}", async (
            Guid id,
            UpdateOrganizationRequest request,
            ICommandHandler<UpdateOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOrganizationCommand(id, request.Name, request.Logo);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .WithName("UpdateOrganization")
        .WithSummary("Update an organization's name and logo.")
        .HasPermission(Permissions.Organizations.Edit)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Organizations.NotFound", "No organization exists with the specified Id.");
    }
}
