using System.Globalization;
using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Tickets.Create;

internal sealed class SendEmailTicketCreatedNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendEmailTicketCreatedNotificationHandler> logger)
    : INotificationEventHandler<TicketCreatedNotification>
{
    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        Ticket? ticket = await context.Tickets
            .Include(t => t.Messages)
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket is null)
        {
            logger.LogWarning("Ticket with Id {TicketId} was not found for notification handling.", notification.TicketId);
            return;
        }

        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == ticket.ProjectId, cancellationToken);

        string projectName = project?.Name ?? "Progetto";
        string ticketUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/tickets/{ticket.Id}";

        User? author = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == ticket.CreatedByUserId, cancellationToken);

        await SendAuthorNotificationAsync(ticket, author, projectName, ticketUrl, cancellationToken);

        await SendSupportNotificationAsync(ticket, author, projectName, ticketUrl, cancellationToken);
    }

    private async Task SendAuthorNotificationAsync(
        Ticket ticket,
        User? author,
        string projectName,
        string ticketUrl,
        CancellationToken cancellationToken)
    {
        if (author is null || string.IsNullOrWhiteSpace(author.Email))
        {
            logger.LogWarning("Ticket author for ticket {TicketId} has no valid email. Skipping author notification.", ticket.Id);
            return;
        }

        string recipientName = $"{author.FirstName} {author.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = author.Email;
        }

        var model = new TicketCreatedAuthorEmailModel(
            RecipientName: recipientName,
            TicketId: ticket.Id,
            Title: ticket.Title,
            Priority: ticket.Priority.ToString(),
            Status: ticket.Status.ToString(),
            ProjectName: projectName,
            TicketUrl: new Uri(ticketUrl),
            CreatedAt: ticket.CreatedAt.ToString("g", CultureInfo.InvariantCulture),
            FirstResponseDueAt: ticket.FirstResponseDueAt?.ToString("g", CultureInfo.InvariantCulture));

        string htmlBody = await templateRenderer.RenderAsync("ticket-created-author", model, cancellationToken);

        await emailSender.SendAsync(
            author.Email,
            $"[Ticket #{ticket.Id}] {ticket.Title}",
            htmlBody,
            cancellationToken);
    }

    private async Task SendSupportNotificationAsync(
        Ticket ticket,
        User? author,
        string projectName,
        string ticketUrl,
        CancellationToken cancellationToken)
    {
        List<User> supportUsers = await (
            from pa in context.ProjectAssignments.AsNoTracking()
            join u in context.Users.AsNoTracking() on pa.UserId equals u.Id
            where pa.ProjectId == ticket.ProjectId && pa.Role == RoleNames.Support
            select u
        ).ToListAsync(cancellationToken);

        List<User> adminUsers = await (
            from ur in context.UserRoles.AsNoTracking()
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join u in context.Users.AsNoTracking() on ur.UserId equals u.Id
            where r.Name == RoleNames.Admin
            select u
        ).ToListAsync(cancellationToken);

        var supportUserIds = supportUsers.Select(u => u.Id).ToHashSet();
        List<User> allStaff = [.. supportUsers, .. adminUsers.Where(a => !supportUserIds.Contains(a.Id))];

        if (allStaff.Count == 0)
        {
            logger.LogWarning("No support staff or admin users found to notify for ticket {TicketId}.", ticket.Id);
            return;
        }

        string authorName = author is not null ? $"{author.FirstName} {author.LastName}".Trim() : "Utente";
        if (string.IsNullOrWhiteSpace(authorName) && author is not null)
        {
            authorName = author.Email ?? "Utente";
        }

        string authorEmail = author?.Email ?? string.Empty;
        string initialMessage = ticket.Messages.OrderBy(m => m.CreatedAt).FirstOrDefault()?.Content ?? string.Empty;

        foreach (User user in allStaff)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            bool isAdmin = !supportUserIds.Contains(user.Id);

            string recipientName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                recipientName = user.Email;
            }

            var model = new TicketCreatedSupportEmailModel(
                RecipientName: recipientName,
                TicketId: ticket.Id,
                Title: ticket.Title,
                Priority: ticket.Priority.ToString(),
                ProjectName: projectName,
                AuthorName: authorName,
                AuthorEmail: authorEmail,
                InitialMessage: initialMessage,
                TicketUrl: new Uri(ticketUrl),
                CreatedAt: ticket.CreatedAt.ToString("g", CultureInfo.InvariantCulture),
                FirstResponseDueAt: ticket.FirstResponseDueAt?.ToString("g", CultureInfo.InvariantCulture),
                IsAdminFallback: isAdmin);

            string htmlBody = await templateRenderer.RenderAsync("ticket-created-support", model, cancellationToken);

            string subjectPrefix = isAdmin ? "[Amministrazione - Nuovo Ticket" : "[Supporto - Nuovo Ticket";
            await emailSender.SendAsync(
                user.Email,
                $"{subjectPrefix} #{ticket.Id}] {ticket.Title}",
                htmlBody,
                cancellationToken);
        }
    }
}
