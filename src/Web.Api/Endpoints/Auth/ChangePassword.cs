using Application.Abstractions.Messaging;
using Application.Users.ChangePassword;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class ChangePassword : IEndpoint
{
    public sealed record Request(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/change-password", async (
            Request request,
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
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
