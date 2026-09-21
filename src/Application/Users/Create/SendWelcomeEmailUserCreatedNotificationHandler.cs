using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Users.Create;

internal sealed class SendWelcomeEmailUserCreatedNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendWelcomeEmailUserCreatedNotificationHandler> logger)
    : INotificationEventHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("User {UserId} was not found or has no email for welcome notification.", notification.UserId);
            return;
        }

        string recipientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = user.Email;
        }

        string loginUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/login";

        var model = new UserWelcomeEmailModel(
            RecipientName: recipientName,
            Email: user.Email,
            LoginUrl: new Uri(loginUrl));

        string htmlBody = await templateRenderer.RenderAsync("user-welcome", model, cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "Benvenuto su OverTicketing",
            htmlBody,
            cancellationToken);
    }
}
