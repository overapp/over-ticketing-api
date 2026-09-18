using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Projects.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Projects;

internal sealed class Create : IEndpoint
{
    public sealed record Request(Guid OrganizationId, string Name, string Description);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("projects", async (
            Request request,
            ICommandHandler<CreateProjectCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectCommand(request.OrganizationId, request.Name, request.Description);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Projects)
        .HasPermission(Permissions.Projects.Create);
    }
}
