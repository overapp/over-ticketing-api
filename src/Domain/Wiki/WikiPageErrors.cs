using SharedKernel;

namespace Domain.Wiki;

public static class WikiPageErrors
{
    public static Error NotFound(Guid pageId) => Error.NotFound(
        "WikiPages.NotFound",
        $"The wiki page with the Id = '{pageId}' was not found");

    public static Error NotFoundBySlug(string slug) => Error.NotFound(
        "WikiPages.NotFoundBySlug",
        $"The wiki page with the slug '{slug}' was not found");

    public static Error SlugAlreadyExists(string slug) => Error.Conflict(
        "WikiPages.SlugAlreadyExists",
        $"A wiki page with the slug '{slug}' already exists in this scope");

    public static Error ParentNotFound(Guid parentPageId) => Error.NotFound(
        "WikiPages.ParentNotFound",
        $"The parent wiki page with the Id = '{parentPageId}' was not found");

    public static Error ParentScopeMismatch => Error.Problem(
        "WikiPages.ParentScopeMismatch",
        "The parent page belongs to a different project or scope");

    public static Error CircularHierarchy => Error.Problem(
        "WikiPages.CircularHierarchy",
        "A wiki page cannot be its own ancestor or parent");

    public static Error HasChildPages(Guid pageId) => Error.Problem(
        "WikiPages.HasChildPages",
        $"The wiki page with the Id = '{pageId}' cannot be deleted or archived because it has child pages");

    public static Error UnauthorizedAccess => Error.Problem(
        "WikiPages.UnauthorizedAccess",
        "You do not have permission to access or modify this wiki page");

    public static Error GlobalPagesRequireAdmin => Error.Problem(
        "WikiPages.GlobalPagesRequireAdmin",
        "Only system administrators can create or edit global wiki pages");
}
