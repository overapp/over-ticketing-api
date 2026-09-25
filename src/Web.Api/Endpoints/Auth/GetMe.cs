using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.GetMe;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class GetMe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/me", async (
            IQueryHandler<GetMyUserQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMyUserQuery();

            Result<UserResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Auth)
        .WithName("GetCurrentUser")
        .WithSummary("Retrieve the profile of the currently authenticated user.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists for the current caller.");
    }
}
