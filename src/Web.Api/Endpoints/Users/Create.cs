using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Create : IEndpoint
{
    public sealed record CreateUserRequest(
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyCollection<string>? Roles = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users", async (
            CreateUserRequest request,
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
        .WithTags(Tags.Users)
        .WithName("CreateUser")
        .WithSummary("Create a new user with a system-generated temporary password.")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status409Conflict, "Users.DuplicateEmail", "A user with the provided email already exists.");
    }
}
