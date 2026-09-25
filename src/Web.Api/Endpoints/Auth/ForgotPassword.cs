using Application.Abstractions.Messaging;
using Application.Users.ForgotPassword;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class ForgotPassword : IEndpoint
{
    public sealed record ForgotPasswordRequest(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/forgot-password", async (
            ForgotPasswordRequest request,
            ICommandHandler<ForgotPasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ForgotPasswordCommand(request.Email);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .WithName("ForgotPassword")
        .WithSummary("Request a password reset email for the given address.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError();
    }
}
