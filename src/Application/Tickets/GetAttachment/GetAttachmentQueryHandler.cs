using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.GetAttachment;

internal sealed class GetAttachmentQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetAttachmentQuery, AttachmentDownloadResponse>
{
    public async Task<Result<AttachmentDownloadResponse>> Handle(
        GetAttachmentQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        TicketAttachment? attachment = await context.TicketAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == query.AttachmentId, cancellationToken);

        if (attachment is null)
        {
            return Result.Failure<AttachmentDownloadResponse>(
                Error.NotFound("Attachments.NotFound", $"The attachment with Id = '{query.AttachmentId}' was not found."));
        }

        TicketMessage? message = await context.TicketMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == attachment.TicketMessageId, cancellationToken);

        if (message is null)
        {
            return Result.Failure<AttachmentDownloadResponse>(TicketErrors.MessageNotFound(attachment.TicketMessageId));
        }

        Ticket? ticket = await context.Tickets
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == message.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<AttachmentDownloadResponse>(TicketErrors.NotFound(message.TicketId));
        }

        ProjectAssignment? assignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == userId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<AttachmentDownloadResponse>(TicketErrors.UserNotInProject(userId, ticket.ProjectId));
        }

        bool isSupportOrAdmin = string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isSupportOrAdmin)
        {
            if (ticket.CreatedByUserId != userId)
            {
                return Result.Failure<AttachmentDownloadResponse>(TicketErrors.UnauthorizedAccess(ticket.Id));
            }

            if (message.IsInternal)
            {
                return Result.Failure<AttachmentDownloadResponse>(TicketErrors.UnauthorizedAccess(ticket.Id));
            }
        }

        Stream? stream = await fileStorageService.DownloadAsync(attachment.StoragePath, cancellationToken);
        if (stream is null)
        {
            return Result.Failure<AttachmentDownloadResponse>(
                Error.NotFound("Attachments.FileNotFound", "The attachment file was not found on storage."));
        }

        return new AttachmentDownloadResponse(stream, attachment.FileName, attachment.ContentType);
    }
}
