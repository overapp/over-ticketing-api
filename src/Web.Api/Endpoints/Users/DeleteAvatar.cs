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
            .RequireAuthorization()
            .HasPermission(Permissions.Users.Edit);
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
