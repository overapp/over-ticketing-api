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
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid userId,
        ICommandHandler<DeleteUserAvatarCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserAvatarCommand(userId);

        Result result = await handler.Handle(command, cancellationToken);

        return result.Match(
            () => Results.NoContent(),
            failure => CustomResults.Problem(failure));
    }
}
