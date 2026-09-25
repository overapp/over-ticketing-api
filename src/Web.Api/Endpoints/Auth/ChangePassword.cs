using Application.Abstractions.Messaging;
using Application.Users.ChangePassword;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class ChangePassword : IEndpoint
{
    public sealed record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/change-password", async (
            ChangePasswordRequest request,
            ICommandHandler<ChangePasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Auth)
        .WithName("ChangePassword")
        .WithSummary("Change the authenticated user's password.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists for the current caller.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.UserLockedOut", "The user account is locked out due to too many failed attempts.")
        .ProducesError(StatusCodes.Status400BadRequest, "Users.InvalidCurrentPassword", "The current password provided is incorrect.");
    }
}
