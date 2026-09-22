using Application.Abstractions.Messaging;
using Application.Users.GetAvatar;
using SharedKernel;
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
        .AllowAnonymous();
    }
}
