using System.Globalization;
using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.Users.ChangePassword;

internal sealed class SendPasswordChangedEmailNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IDateTimeProvider dateTimeProvider,
    ILogger<SendPasswordChangedEmailNotificationHandler> logger)
    : INotificationEventHandler<UserPasswordChangedNotification>
{
    public async Task Handle(UserPasswordChangedNotification notification, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("User {UserId} was not found or has no email for password changed notification.", notification.UserId);
            return;
        }

        string recipientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = user.Email;
        }

        var model = new UserPasswordChangedEmailModel(
            RecipientName: recipientName,
            Email: user.Email,
            ChangedAtUtc: dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

        string htmlBody = await templateRenderer.RenderAsync("user-password-changed", model, cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "La tua password di OverTicketing è stata modificata",
            htmlBody,
            cancellationToken);
    }
}
