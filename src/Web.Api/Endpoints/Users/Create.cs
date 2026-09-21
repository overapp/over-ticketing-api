using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Create : IEndpoint
{
    public sealed record Request(
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyCollection<string>? Roles = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users", async (
            Request request,
            ICommandHandler<CreateUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Roles);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .HasPermission(Permissions.Users.Create)
        .WithTags(Tags.Users);
    }
}
