using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Projects;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Projects.AssignUser;

internal sealed class SendEmailUserAssignedToProjectNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendEmailUserAssignedToProjectNotificationHandler> logger)
    : INotificationEventHandler<UserAssignedToProjectNotification>
{
    public async Task Handle(UserAssignedToProjectNotification notification, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Assigned user {UserId} not found or has no email.", notification.UserId);
            return;
        }

        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == notification.ProjectId, cancellationToken);

        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found for assignment notification.", notification.ProjectId);
            return;
        }

        string recipientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = user.Email;
        }

        string projectUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/projects/{project.Id}";

        var model = new ProjectAssignedEmailModel(
            RecipientName: recipientName,
            ProjectName: project.Name,
            Role: notification.Role,
            ProjectUrl: new Uri(projectUrl));

        string htmlBody = await templateRenderer.RenderAsync("project-assigned", model, cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            $"[Progetto: {project.Name}] Assegnazione al progetto",
            htmlBody,
            cancellationToken);
    }
}
