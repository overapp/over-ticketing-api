using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Users.AdminResetPassword;

internal sealed class SendTemporaryPasswordEmailNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendTemporaryPasswordEmailNotificationHandler> logger)
    : INotificationEventHandler<UserTemporaryPasswordAssignedNotification>
{
    public async Task Handle(UserTemporaryPasswordAssignedNotification notification, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("User {UserId} was not found or has no email for temporary password notification.", notification.UserId);
            return;
        }

        string recipientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = user.Email;
        }

        string loginUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/login";

        var model = new UserTemporaryPasswordEmailModel(
            RecipientName: recipientName,
            Email: user.Email,
            LoginUrl: new Uri(loginUrl),
            TemporaryPassword: notification.TemporaryPassword);

        string htmlBody = await templateRenderer.RenderAsync("user-temporary-password", model, cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "Reset password OverTicketing",
            htmlBody,
            cancellationToken);
    }
}
