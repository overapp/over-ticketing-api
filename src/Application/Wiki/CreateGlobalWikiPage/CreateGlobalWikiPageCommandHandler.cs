using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wiki.CreateGlobalWikiPage;

internal sealed class CreateGlobalWikiPageCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateGlobalWikiPageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateGlobalWikiPageCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Verify user is a global Admin
        bool isGlobalAdmin = await context.UserRoles
            .Join(
                context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == RoleNames.Admin, cancellationToken);

        if (!isGlobalAdmin)
        {
            return Result.Failure<Guid>(WikiPageErrors.GlobalPagesRequireAdmin);
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

            // Must also be a global page
            if (parentPage.ProjectId.HasValue)
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
            .AnyAsync(wp => wp.ProjectId == null && wp.Slug == targetSlug, cancellationToken);

        if (slugExists)
        {
            return Result.Failure<Guid>(WikiPageErrors.SlugAlreadyExists(targetSlug));
        }

        DateTime now = dateTimeProvider.UtcNow;

        var wikiPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
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
