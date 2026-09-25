using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Login;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class Login : IEndpoint
{
    public sealed record LoginRequest(string Email, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/login", async (
            LoginRequest request,
            ICommandHandler<LoginUserCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginUserCommand(request.Email, request.Password);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .WithName("Login")
        .WithSummary("Authenticate with email and password and receive access and refresh tokens.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces<AccessTokensResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFoundByEmail", "The email or password is incorrect.");
    }
}
