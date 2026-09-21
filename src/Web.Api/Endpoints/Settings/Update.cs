using Application.Abstractions.Messaging;
using Application.Settings.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Settings;

internal sealed class Update : IEndpoint
{
    public sealed record EmailNotificationSettingsRequest(
        bool NotifyOnTicketCreated,
        bool NotifyOnTicketReply);

    public sealed record Request(
        EmailNotificationSettingsRequest EmailNotifications);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("settings", async (
            Request request,
            ICommandHandler<UpdateUserSettingsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserSettingsCommand(
                new UpdateEmailNotificationSettings(
                    request.EmailNotifications.NotifyOnTicketCreated,
                    request.EmailNotifications.NotifyOnTicketReply));

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Settings)
        .RequireAuthorization();
    }
}
