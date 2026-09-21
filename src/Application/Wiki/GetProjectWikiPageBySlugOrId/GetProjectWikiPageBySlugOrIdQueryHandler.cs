using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.GetProjectWikiPageBySlugOrId;

internal sealed class GetProjectWikiPageBySlugOrIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectWikiPageBySlugOrIdQuery, ProjectWikiPageResponse>
{
    public async Task<Result<ProjectWikiPageResponse>> Handle(
        GetProjectWikiPageBySlugOrIdQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify project exists
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectWikiPageResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        // Check if user is global Admin
        bool isGlobalAdmin = await context.UserRoles
            .Join(
                context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == RoleNames.Admin, cancellationToken);

        ProjectAssignment? assignment = null;
        if (!isGlobalAdmin)
        {
            assignment = await context.ProjectAssignments
                .AsNoTracking()
                .SingleOrDefaultAsync(pa => pa.ProjectId == query.ProjectId && pa.UserId == userId, cancellationToken);

            if (assignment is null)
            {
                return Result.Failure<ProjectWikiPageResponse>(WikiPageErrors.UnauthorizedAccess);
            }
        }

        bool isSupportOrAdmin = isGlobalAdmin ||
            string.Equals(assignment?.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assignment?.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        // Fetch page by Id (if GUID) or Slug
        bool isGuid = Guid.TryParse(query.SlugOrId, out Guid pageId);

        IQueryable<WikiPage> pageQuery = context.WikiPages
            .AsNoTracking()
            .Where(wp => wp.ProjectId == query.ProjectId);

        pageQuery = isGuid
            ? pageQuery.Where(wp => wp.Id == pageId)
            : pageQuery.Where(wp => wp.Slug == query.SlugOrId);

        WikiPage? page = await pageQuery.SingleOrDefaultAsync(cancellationToken);

        if (page is null)
        {
            return Result.Failure<ProjectWikiPageResponse>(
                isGuid ? WikiPageErrors.NotFound(pageId) : WikiPageErrors.NotFoundBySlug(query.SlugOrId));
        }

        // Standard user can only read Published non-internal pages
        if (!isSupportOrAdmin && (page.Status != WikiPageStatus.Published || page.IsInternalOnly))
        {
            return Result.Failure<ProjectWikiPageResponse>(WikiPageErrors.UnauthorizedAccess);
        }

        return new ProjectWikiPageResponse
        {
            Id = page.Id,
            ProjectId = page.ProjectId,
            ParentPageId = page.ParentPageId,
            Title = page.Title,
            Slug = page.Slug,
            Content = page.Content,
            Status = page.Status.ToString(),
            IsInternalOnly = page.IsInternalOnly,
            CreatedByUserId = page.CreatedByUserId,
            CreatedAt = page.CreatedAt,
            UpdatedByUserId = page.UpdatedByUserId,
            UpdatedAt = page.UpdatedAt
        };
    }
}
