using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Update : IEndpoint
{
    public sealed record UpdateUserRequest(
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyCollection<string>? Roles = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId:guid}", async (
            Guid userId,
            UpdateUserRequest request,
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
        .WithTags(Tags.Users)
        .WithName("UpdateUser")
        .WithSummary("Update a user's profile and role assignments.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists with the specified Id.")
        .ProducesError(StatusCodes.Status409Conflict, "Users.DuplicateEmail", "Another user already uses the provided email.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.CannotDemoteSelf", "You cannot revoke your own administrator role.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.CannotDemoteLastAdmin", "The last administrator account cannot be demoted.");
    }
}
