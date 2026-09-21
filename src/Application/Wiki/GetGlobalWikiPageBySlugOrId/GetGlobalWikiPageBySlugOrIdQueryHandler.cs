using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.GetGlobalWikiPageBySlugOrId;

internal sealed class GetGlobalWikiPageBySlugOrIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetGlobalWikiPageBySlugOrIdQuery, GlobalWikiPageResponse>
{
    public async Task<Result<GlobalWikiPageResponse>> Handle(
        GetGlobalWikiPageBySlugOrIdQuery query,
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

        bool isGuid = Guid.TryParse(query.SlugOrId, out Guid pageId);

        IQueryable<WikiPage> pageQuery = context.WikiPages
            .AsNoTracking()
            .Where(wp => wp.ProjectId == null);

        pageQuery = isGuid
            ? pageQuery.Where(wp => wp.Id == pageId)
            : pageQuery.Where(wp => wp.Slug == query.SlugOrId);

        WikiPage? page = await pageQuery.SingleOrDefaultAsync(cancellationToken);

        if (page is null)
        {
            return Result.Failure<GlobalWikiPageResponse>(
                isGuid ? WikiPageErrors.NotFound(pageId) : WikiPageErrors.NotFoundBySlug(query.SlugOrId));
        }

        // Standard user can only read Published non-internal pages
        if (!isSupportOrAdmin && (page.Status != WikiPageStatus.Published || page.IsInternalOnly))
        {
            return Result.Failure<GlobalWikiPageResponse>(WikiPageErrors.UnauthorizedAccess);
        }

        return new GlobalWikiPageResponse
        {
            Id = page.Id,
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
