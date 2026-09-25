using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Create : IEndpoint
{
    public sealed record CreateOrganizationRequest(string Name, string Logo);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("organizations", async (
            CreateOrganizationRequest request,
            ICommandHandler<CreateOrganizationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrganizationCommand(request.Name, request.Logo);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .WithName("CreateOrganization")
        .WithSummary("Create a new organization.")
        .HasPermission(Permissions.Organizations.Create)
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationError();
    }
}
