using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.GetGlobalWikiPages;

internal sealed class GetGlobalWikiPagesQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetGlobalWikiPagesQuery, PagedResponse<GlobalWikiPageSummaryResponse>>
{
    public async Task<Result<PagedResponse<GlobalWikiPageSummaryResponse>>> Handle(
        GetGlobalWikiPagesQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Check user roles (Admin or Support can see internal pages & drafts)
        List<string> userRoles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name!)
            .ToListAsync(cancellationToken);

        bool isSupportOrAdmin = userRoles.Any(r =>
            string.Equals(r, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleNames.Support, StringComparison.OrdinalIgnoreCase));

        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<WikiPage> pagesQuery = context.WikiPages
            .AsNoTracking()
            .Where(wp => wp.ProjectId == null);

        // Standard user can only view Published non-internal pages
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

        List<GlobalWikiPageSummaryResponse> items = await pagesQuery
            .OrderBy(wp => wp.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wp => new GlobalWikiPageSummaryResponse
            {
                Id = wp.Id,
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

        return new PagedResponse<GlobalWikiPageSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
