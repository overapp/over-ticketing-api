using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.AdminResetPassword;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ResetPasswordByAdmin : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/reset-password", async (
            Guid userId,
            ICommandHandler<AdminResetPasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AdminResetPasswordCommand(userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .HasPermission(Permissions.Users.Edit)
        .WithTags(Tags.Users);
    }
}
