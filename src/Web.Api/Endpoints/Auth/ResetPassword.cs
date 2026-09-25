using Application.Abstractions.Messaging;
using Application.Users.ResetPassword;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class ResetPassword : IEndpoint
{
    public sealed record ResetPasswordRequest(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/reset-password", async (
            ResetPasswordRequest request,
            ICommandHandler<ResetPasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(
                request.Email,
                request.Token,
                request.NewPassword,
                request.ConfirmPassword);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .WithName("ResetPassword")
        .WithSummary("Set a new password using a password reset token.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status400BadRequest, "Users.InvalidPasswordResetToken", "The provided password reset token is invalid, expired, or the email does not match an existing user.");
    }
}
