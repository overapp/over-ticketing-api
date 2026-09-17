using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Organizations.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

internal sealed class Update : IEndpoint
{
    public sealed record Request(string Name, string Logo);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("organizations/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOrganizationCommand(id, request.Name, request.Logo);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Organizations)
        .HasPermission(Permissions.Organizations.Edit);
    }
}
