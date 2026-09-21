using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.UpdateWikiPageStatus;

internal sealed class UpdateWikiPageStatusCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateWikiPageStatusCommand>
{
    public async Task<Result> Handle(
        UpdateWikiPageStatusCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        WikiPage? page = await context.WikiPages
            .SingleOrDefaultAsync(wp => wp.Id == command.WikiPageId, cancellationToken);

        if (page is null)
        {
            return Result.Failure(WikiPageErrors.NotFound(command.WikiPageId));
        }

        bool isGlobalAdmin = await context.UserRoles
            .Join(
                context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == RoleNames.Admin, cancellationToken);

        // If it's a global page, only global Admin can change status
        if (!page.ProjectId.HasValue)
        {
            if (!isGlobalAdmin)
            {
                return Result.Failure(WikiPageErrors.GlobalPagesRequireAdmin);
            }
        }
        else if (!isGlobalAdmin)
        {
            // For project page, user must be Support or Admin in this project
            ProjectAssignment? assignment = await context.ProjectAssignments
                .AsNoTracking()
                .SingleOrDefaultAsync(pa => pa.ProjectId == page.ProjectId.Value && pa.UserId == userId, cancellationToken);

            if (assignment is null ||
                !string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(WikiPageErrors.UnauthorizedAccess);
            }
        }

        // If archiving, check that there are no active (non-archived) child pages
        if (command.Status == WikiPageStatus.Archived)
        {
            bool hasActiveChildPages = await context.WikiPages
                .AnyAsync(wp => wp.ParentPageId == page.Id && wp.Status != WikiPageStatus.Archived, cancellationToken);

            if (hasActiveChildPages)
            {
                return Result.Failure(WikiPageErrors.HasChildPages(page.Id));
            }
        }

        DateTime now = dateTimeProvider.UtcNow;
        page.Status = command.Status;
        page.UpdatedByUserId = userId;
        page.UpdatedAt = now;

        if (command.Status == WikiPageStatus.Archived)
        {
            page.Raise(new WikiPageArchivedDomainEvent(page.Id));
        }
        else
        {
            page.Raise(new WikiPageUpdatedDomainEvent(page.Id));
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
