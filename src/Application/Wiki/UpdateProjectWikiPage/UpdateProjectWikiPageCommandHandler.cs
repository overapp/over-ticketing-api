using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.UpdateProjectWikiPage;

internal sealed class UpdateProjectWikiPageCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateProjectWikiPageCommand>
{
    public async Task<Result> Handle(
        UpdateProjectWikiPageCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify project exists
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        // Check if user is an Admin or assigned to this project with Support or Admin role
        bool isGlobalAdmin = await context.UserRoles
            .Join(
                context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == RoleNames.Admin, cancellationToken);

        if (!isGlobalAdmin)
        {
            ProjectAssignment? assignment = await context.ProjectAssignments
                .AsNoTracking()
                .SingleOrDefaultAsync(pa => pa.ProjectId == command.ProjectId && pa.UserId == userId, cancellationToken);

            if (assignment is null ||
                !string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(WikiPageErrors.UnauthorizedAccess);
            }
        }

        WikiPage? page = await context.WikiPages
            .SingleOrDefaultAsync(wp => wp.Id == command.WikiPageId, cancellationToken);

        if (page is null)
        {
            return Result.Failure(WikiPageErrors.NotFound(command.WikiPageId));
        }

        if (page.ProjectId != command.ProjectId)
        {
            return Result.Failure(WikiPageErrors.NotFound(command.WikiPageId));
        }

        // Validate parent page if provided
        if (command.ParentPageId.HasValue)
        {
            if (command.ParentPageId.Value == command.WikiPageId)
            {
                return Result.Failure(WikiPageErrors.CircularHierarchy);
            }

            WikiPage? parentPage = await context.WikiPages
                .AsNoTracking()
                .SingleOrDefaultAsync(wp => wp.Id == command.ParentPageId.Value, cancellationToken);

            if (parentPage is null)
            {
                return Result.Failure(WikiPageErrors.ParentNotFound(command.ParentPageId.Value));
            }

            if (parentPage.ProjectId != command.ProjectId)
            {
                return Result.Failure(WikiPageErrors.ParentScopeMismatch);
            }
        }

        // Handle slug update if explicitly provided
        if (!string.IsNullOrWhiteSpace(command.Slug))
        {
            string newSlug = SlugHelper.GenerateSlug(command.Slug);
            if (!string.Equals(newSlug, page.Slug, StringComparison.OrdinalIgnoreCase))
            {
                bool slugExists = await context.WikiPages
                    .AnyAsync(wp => wp.ProjectId == command.ProjectId &&
                                    wp.Slug == newSlug &&
                                    wp.Id != command.WikiPageId,
                              cancellationToken);

                if (slugExists)
                {
                    return Result.Failure(WikiPageErrors.SlugAlreadyExists(newSlug));
                }

                page.Slug = newSlug;
            }
        }

        DateTime now = dateTimeProvider.UtcNow;

        page.Title = command.Title;
        page.Content = command.Content;
        page.ParentPageId = command.ParentPageId;
        page.IsInternalOnly = command.IsInternalOnly;
        page.UpdatedByUserId = userId;
        page.UpdatedAt = now;

        page.Raise(new WikiPageUpdatedDomainEvent(page.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
