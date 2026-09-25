using Application.Abstractions.Authentication;
using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.DeleteAvatar;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class DeleteAvatar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId:guid}/avatar", Handle)
            .WithTags(Tags.Users)
            .WithName("DeleteUserAvatar")
            .WithSummary("Remove the user's profile picture.")
            .RequireAuthorization()
            .HasPermission(Permissions.Users.Edit)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists with the specified Id.")
            .ProducesError(StatusCodes.Status500InternalServerError, "Users.UpdateFailed", "Failed to update the user while removing the avatar.");
    }

    private static async Task<IResult> Handle(
        Guid userId,
        IUserContext userContext,
        ICommandHandler<DeleteUserAvatarCommand> handler,
        CancellationToken cancellationToken)
    {
        if (userId != userContext.UserId)
        {
            return Results.Forbid();
        }

        var command = new DeleteUserAvatarCommand(userId);

        Result result = await handler.Handle(command, cancellationToken);

        return result.Match(
            () => Results.NoContent(),
            failure => CustomResults.Problem(failure));
    }
}
