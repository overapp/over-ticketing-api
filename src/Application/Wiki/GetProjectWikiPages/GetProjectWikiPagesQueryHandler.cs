using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.GetProjectWikiPages;

internal sealed class GetProjectWikiPagesQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectWikiPagesQuery, PagedResponse<WikiPageSummaryResponse>>
{
    public async Task<Result<PagedResponse<WikiPageSummaryResponse>>> Handle(
        GetProjectWikiPagesQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify project exists
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<PagedResponse<WikiPageSummaryResponse>>(ProjectErrors.NotFound(query.ProjectId));
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
                return Result.Failure<PagedResponse<WikiPageSummaryResponse>>(WikiPageErrors.UnauthorizedAccess);
            }
        }

        bool isSupportOrAdmin = isGlobalAdmin ||
            string.Equals(assignment?.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assignment?.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<WikiPage> pagesQuery = context.WikiPages
            .AsNoTracking()
            .Where(wp => wp.ProjectId == query.ProjectId);

        // Standard users can only view Published non-internal pages
        if (!isSupportOrAdmin)
        {
            pagesQuery = pagesQuery.Where(wp => wp.Status == WikiPageStatus.Published && !wp.IsInternalOnly);
        }
        else if (query.Status.HasValue)
        {
            pagesQuery = pagesQuery.Where(wp => wp.Status == query.Status.Value);
        }

        if (query.ParentPageId.HasValue)
        {
            pagesQuery = pagesQuery.Where(wp => wp.ParentPageId == query.ParentPageId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            pagesQuery = pagesQuery.Where(wp =>
                EF.Functions.Like(wp.Title, term) ||
                EF.Functions.Like(wp.Content, term));
        }

        int totalCount = await pagesQuery.CountAsync(cancellationToken);

        List<WikiPageSummaryResponse> items = await pagesQuery
            .OrderBy(wp => wp.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wp => new WikiPageSummaryResponse
            {
                Id = wp.Id,
                ProjectId = wp.ProjectId,
                ParentPageId = wp.ParentPageId,
                Title = wp.Title,
                Slug = wp.Slug,
                Status = wp.Status.ToString(),
                IsInternalOnly = wp.IsInternalOnly,
                CreatedByUserId = wp.CreatedByUserId,
                CreatedAt = wp.CreatedAt,
                UpdatedByUserId = wp.UpdatedByUserId,
                UpdatedAt = wp.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<WikiPageSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
