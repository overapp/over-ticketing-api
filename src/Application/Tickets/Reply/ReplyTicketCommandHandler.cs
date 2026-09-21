using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.Reply;

internal sealed class ReplyTicketCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReplyTicketCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ReplyTicketCommand command, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Ticket? ticket = await context.Tickets
            .SingleOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<Guid>(TicketErrors.NotFound(command.TicketId));
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return Result.Failure<Guid>(TicketErrors.Closed(command.TicketId));
        }

        // Check project assignment and role
        ProjectAssignment? assignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == userId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<Guid>(TicketErrors.UserNotInProject(userId, ticket.ProjectId));
        }

        bool isSupportOrAdmin = string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        // Standard user can only reply to their own tickets and cannot post internal notes
        if (!isSupportOrAdmin)
        {
            if (ticket.CreatedByUserId != userId)
            {
                return Result.Failure<Guid>(TicketErrors.UnauthorizedAccess(command.TicketId));
            }

            if (command.IsInternal)
            {
                return Result.Failure<Guid>(TicketErrors.UnauthorizedAccess(command.TicketId));
            }
        }

        DateTime now = dateTimeProvider.UtcNow;

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = userId,
            Content = command.Content,
            IsInternal = command.IsInternal,
            CreatedAt = now
        };

        if (command.Attachments is not null)
        {
            foreach (FileUploadModel upload in command.Attachments)
            {
                string storagePath = await fileStorageService.UploadAsync(
                    upload.ContentStream,
                    upload.FileName,
                    upload.ContentType,
                    cancellationToken);

                var attachment = new TicketAttachment
                {
                    Id = Guid.NewGuid(),
                    TicketMessageId = message.Id,
                    FileName = upload.FileName,
                    ContentType = upload.ContentType,
                    FileSizeBytes = upload.Size,
                    StoragePath = storagePath,
                    CreatedAt = now
                };

                message.Attachments.Add(attachment);
            }
        }

        // State transitions & SLA updates
        TicketStatus previousStatus = ticket.Status;
        if (isSupportOrAdmin)
        {
            // If support replies publicly
            if (!command.IsInternal)
            {
                ticket.FirstResponseAt ??= now;

                ticket.Status = TicketStatus.WaitingForCustomer;
            }
        }
        else
        {
            // User replies: if resolved, reopen to WaitingForSupport; else set WaitingForSupport
            ticket.Status = TicketStatus.WaitingForSupport;
            if (previousStatus == TicketStatus.Resolved)
            {
                ticket.ResolvedAt = null;
            }
        }

        ticket.UpdatedAt = now;
        context.TicketMessages.Add(message);

        if (ticket.Status != previousStatus)
        {
            ticket.Raise(new TicketStatusChangedDomainEvent(ticket.Id, previousStatus, ticket.Status));
        }

        ticket.Raise(new TicketMessageAddedDomainEvent(ticket.Id, message.Id, message.IsInternal));

        await context.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
