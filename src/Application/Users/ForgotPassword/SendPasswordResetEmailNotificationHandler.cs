using System.Net;
using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Users.ForgotPassword;

internal sealed class SendPasswordResetEmailNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendPasswordResetEmailNotificationHandler> logger)
    : INotificationEventHandler<UserPasswordResetRequestedNotification>
{
    public async Task Handle(UserPasswordResetRequestedNotification notification, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("User {UserId} was not found or has no email for password reset notification.", notification.UserId);
            return;
        }

        string recipientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = user.Email;
        }

        string encodedEmail = WebUtility.UrlEncode(notification.Email);
        string encodedToken = WebUtility.UrlEncode(notification.ResetToken);
        string resetUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/reset-password?email={encodedEmail}&token={encodedToken}";

        var model = new UserPasswordResetEmailModel(
            RecipientName: recipientName,
            Email: notification.Email,
            ResetUrl: new Uri(resetUrl));

        string htmlBody = await templateRenderer.RenderAsync("user-password-reset", model, cancellationToken);

        await emailSender.SendAsync(
            notification.Email,
            "Recupero password OverTicketing",
            htmlBody,
            cancellationToken);
    }
}
