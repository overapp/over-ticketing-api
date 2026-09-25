using Application.Abstractions.Messaging;
using Application.Users.GetAvatar;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetAvatar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/avatar", async (
            Guid userId,
            IQueryHandler<GetUserAvatarQuery, AvatarDownloadResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserAvatarQuery(userId);

            Result<AvatarDownloadResponse> result = await handler.Handle(query, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            return Results.File(result.Value.Stream, result.Value.ContentType);
        })
        .WithTags(Tags.Users)
        .WithName("GetUserAvatar")
        .WithSummary("Download a user's profile picture.")
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists with the specified Id.")
        .ProducesError(StatusCodes.Status404NotFound, "Users.AvatarNotFound", "The user has no avatar uploaded.")
        .ProducesError(StatusCodes.Status404NotFound, "Users.AvatarFileNotFound", "The avatar file was not found on storage.");
    }
}
