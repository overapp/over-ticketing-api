using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Update : IEndpoint
{
    public sealed record Request(
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyCollection<string>? Roles = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId:guid}", async (
            Guid userId,
            Request request,
            ICommandHandler<UpdateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserCommand(
                userId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Roles);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .HasPermission(Permissions.Users.Edit)
        .WithTags(Tags.Users);
    }
}
