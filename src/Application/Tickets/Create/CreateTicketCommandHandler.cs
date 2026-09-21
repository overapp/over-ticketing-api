using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Projects;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.Create;

internal sealed class CreateTicketCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateTicketCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify project exists and is active
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<Guid>(ProjectErrors.NotFound(command.ProjectId));
        }

        if (project.Status != ProjectStatus.Active)
        {
            return Result.Failure<Guid>(ProjectErrors.AlreadyArchived(command.ProjectId));
        }

        // Verify user is assigned to this project
        bool isAssigned = await context.ProjectAssignments
            .AnyAsync(pa => pa.ProjectId == command.ProjectId && pa.UserId == userId, cancellationToken);

        if (!isAssigned)
        {
            return Result.Failure<Guid>(TicketErrors.UserNotInProject(userId, command.ProjectId));
        }

        DateTime now = dateTimeProvider.UtcNow;
        (DateTime firstResponseDueAt, DateTime resolutionDueAt) = TicketSlaCalculator.CalculateDueDates(now, command.Priority);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            CreatedByUserId = userId,
            Title = command.Title,
            Status = TicketStatus.New,
            Priority = command.Priority,
            CreatedAt = now,
            FirstResponseDueAt = firstResponseDueAt,
            ResolutionDueAt = resolutionDueAt
        };

        var initialMessage = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = userId,
            Content = command.Message,
            IsInternal = false,
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
                    TicketMessageId = initialMessage.Id,
                    FileName = upload.FileName,
                    ContentType = upload.ContentType,
                    FileSizeBytes = upload.Size,
                    StoragePath = storagePath,
                    CreatedAt = now
                };

                initialMessage.Attachments.Add(attachment);
            }
        }

        ticket.Messages.Add(initialMessage);
        ticket.Raise(new TicketCreatedDomainEvent(ticket.Id));

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}
