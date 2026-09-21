using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.CreateProjectWikiPage;

internal sealed class CreateProjectWikiPageCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateProjectWikiPageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProjectWikiPageCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify project exists
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<Guid>(ProjectErrors.NotFound(command.ProjectId));
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
                return Result.Failure<Guid>(WikiPageErrors.UnauthorizedAccess);
            }
        }

        // Validate parent page if provided
        if (command.ParentPageId.HasValue)
        {
            WikiPage? parentPage = await context.WikiPages
                .AsNoTracking()
                .SingleOrDefaultAsync(wp => wp.Id == command.ParentPageId.Value, cancellationToken);

            if (parentPage is null)
            {
                return Result.Failure<Guid>(WikiPageErrors.ParentNotFound(command.ParentPageId.Value));
            }

            if (parentPage.ProjectId != command.ProjectId)
            {
                return Result.Failure<Guid>(WikiPageErrors.ParentScopeMismatch);
            }
        }

        // Calculate slug
        string targetSlug = !string.IsNullOrWhiteSpace(command.Slug)
            ? SlugHelper.GenerateSlug(command.Slug)
            : SlugHelper.GenerateSlug(command.Title);

        if (string.IsNullOrWhiteSpace(targetSlug))
        {
            targetSlug = Guid.NewGuid().ToString("N")[..8];
        }

        bool slugExists = await context.WikiPages
            .AnyAsync(wp => wp.ProjectId == command.ProjectId && wp.Slug == targetSlug, cancellationToken);

        if (slugExists)
        {
            return Result.Failure<Guid>(WikiPageErrors.SlugAlreadyExists(targetSlug));
        }

        DateTime now = dateTimeProvider.UtcNow;

        var wikiPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            ParentPageId = command.ParentPageId,
            Title = command.Title,
            Slug = targetSlug,
            Content = command.Content,
            Status = WikiPageStatus.Draft,
            IsInternalOnly = command.IsInternalOnly,
            CreatedByUserId = userId,
            CreatedAt = now
        };

        wikiPage.Raise(new WikiPageCreatedDomainEvent(wikiPage.Id));

        context.WikiPages.Add(wikiPage);
        await context.SaveChangesAsync(cancellationToken);

        return wikiPage.Id;
    }
}
