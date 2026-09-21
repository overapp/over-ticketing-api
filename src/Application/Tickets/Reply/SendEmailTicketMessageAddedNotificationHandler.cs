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

namespace Application.Tickets.Reply;

internal sealed class SendEmailTicketMessageAddedNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailNotificationOptions> emailOptions,
    ILogger<SendEmailTicketMessageAddedNotificationHandler> logger)
    : INotificationEventHandler<TicketMessageAddedNotification>
{
    public async Task Handle(TicketMessageAddedNotification notification, CancellationToken cancellationToken)
    {
        // Internal notes are staff-only and should never trigger notifications to the customer
        if (notification.IsInternal)
        {
            return;
        }

        Ticket? ticket = await context.Tickets
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket is null)
        {
            logger.LogWarning("Ticket {TicketId} was not found for message notification.", notification.TicketId);
            return;
        }

        TicketMessage? message = await context.TicketMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == notification.MessageId, cancellationToken);

        if (message is null)
        {
            logger.LogWarning("Message {MessageId} was not found for notification.", notification.MessageId);
            return;
        }

        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == ticket.ProjectId, cancellationToken);

        string projectName = project?.Name ?? "Progetto";
        string ticketUrl = $"{emailOptions.Value.AppBaseUrl.TrimEnd('/')}/tickets/{ticket.Id}";

        User? messageAuthor = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == message.AuthorUserId, cancellationToken);

        bool isSupportOrAdmin = await context.ProjectAssignments
            .AsNoTracking()
            .AnyAsync(pa => pa.ProjectId == ticket.ProjectId &&
                            pa.UserId == message.AuthorUserId &&
                            (pa.Role == RoleNames.Support || pa.Role == RoleNames.Admin), cancellationToken);

        if (!isSupportOrAdmin)
        {
            isSupportOrAdmin = await (
                from ur in context.UserRoles.AsNoTracking()
                join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ur.UserId == message.AuthorUserId && r.Name == RoleNames.Admin
                select ur
            ).AnyAsync(cancellationToken);
        }

        if (isSupportOrAdmin)
        {
            await NotifyCustomerAsync(ticket, message, messageAuthor, projectName, ticketUrl, cancellationToken);
        }
        else
        {
            await NotifyStaffAsync(ticket, message, messageAuthor, projectName, ticketUrl, cancellationToken);
        }
    }

    private async Task NotifyCustomerAsync(
        Ticket ticket,
        TicketMessage message,
        User? staffAuthor,
        string projectName,
        string ticketUrl,
        CancellationToken cancellationToken)
    {
        User? customer = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == ticket.CreatedByUserId, cancellationToken);

        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            logger.LogWarning("Customer for ticket {TicketId} has no valid email. Skipping reply notification.", ticket.Id);
            return;
        }

        string recipientName = $"{customer.FirstName} {customer.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            recipientName = customer.Email;
        }

        string staffName = staffAuthor is not null
            ? $"{staffAuthor.FirstName} {staffAuthor.LastName}".Trim()
            : "Team di supporto";

        if (string.IsNullOrWhiteSpace(staffName))
        {
            staffName = staffAuthor?.Email ?? "Team di supporto";
        }

        var model = new TicketReplyCustomerEmailModel(
            RecipientName: recipientName,
            TicketId: ticket.Id,
            Title: ticket.Title,
            ProjectName: projectName,
            RepliedByName: staffName,
            MessageContent: message.Content,
            TicketUrl: new Uri(ticketUrl),
            RepliedAt: message.CreatedAt.ToString("g", CultureInfo.InvariantCulture));

        string htmlBody = await templateRenderer.RenderAsync("ticket-reply-customer", model, cancellationToken);

        await emailSender.SendAsync(
            customer.Email,
            $"[Risposta Ticket #{ticket.Id}] {ticket.Title}",
            htmlBody,
            cancellationToken);
    }

    private async Task NotifyStaffAsync(
        Ticket ticket,
        TicketMessage message,
        User? userAuthor,
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
            logger.LogWarning("No staff users found to notify for ticket reply {TicketId}.", ticket.Id);
            return;
        }

        string authorName = userAuthor is not null
            ? $"{userAuthor.FirstName} {userAuthor.LastName}".Trim()
            : "Utente";

        if (string.IsNullOrWhiteSpace(authorName))
        {
            authorName = userAuthor?.Email ?? "Utente";
        }

        string authorEmail = userAuthor?.Email ?? string.Empty;

        foreach (User staffMember in allStaff)
        {
            if (string.IsNullOrWhiteSpace(staffMember.Email))
            {
                continue;
            }

            bool isAdmin = !supportUserIds.Contains(staffMember.Id);

            string recipientName = $"{staffMember.FirstName} {staffMember.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                recipientName = staffMember.Email;
            }

            var model = new TicketReplyStaffEmailModel(
                RecipientName: recipientName,
                TicketId: ticket.Id,
                Title: ticket.Title,
                ProjectName: projectName,
                AuthorName: authorName,
                AuthorEmail: authorEmail,
                MessageContent: message.Content,
                TicketUrl: new Uri(ticketUrl),
                RepliedAt: message.CreatedAt.ToString("g", CultureInfo.InvariantCulture),
                IsAdmin: isAdmin);

            string htmlBody = await templateRenderer.RenderAsync("ticket-reply-staff", model, cancellationToken);

            string subjectPrefix = isAdmin ? "[Amministrazione - Nuova Risposta" : "[Supporto - Nuova Risposta";
            await emailSender.SendAsync(
                staffMember.Email,
                $"{subjectPrefix} #{ticket.Id}] {ticket.Title}",
                htmlBody,
                cancellationToken);
        }
    }
}
