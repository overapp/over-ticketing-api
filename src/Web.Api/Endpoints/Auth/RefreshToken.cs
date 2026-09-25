using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Refresh;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class RefreshToken : IEndpoint
{
    public sealed record RefreshTokenRequest(string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/refresh-token", async (
            RefreshTokenRequest request,
            ICommandHandler<RefreshTokenCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .WithName("RefreshToken")
        .WithSummary("Exchange a refresh token for a new access and refresh token pair.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces<AccessTokensResponse>(StatusCodes.Status200OK)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status400BadRequest, "Users.InvalidRefreshToken", "The provided refresh token is invalid or has expired.");
    }
}
