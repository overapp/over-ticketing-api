using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId:guid}", async (
            Guid userId,
            ICommandHandler<DeleteUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteUserCommand(userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .HasPermission(Permissions.Users.Delete)
        .WithTags(Tags.Users)
        .WithName("DeleteUser")
        .WithSummary("Permanently delete a user account.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.CannotDeleteSelf", "You cannot delete your own user account.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.CannotDeleteLastAdmin", "The last administrator account cannot be deleted.");
    }
}
