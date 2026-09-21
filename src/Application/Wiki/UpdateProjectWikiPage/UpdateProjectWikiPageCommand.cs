using Application.Abstractions.Messaging;

namespace Application.Wiki.UpdateProjectWikiPage;

public sealed record UpdateProjectWikiPageCommand(
    Guid ProjectId,
    Guid WikiPageId,
    string Title,
    string? Slug,
    string Content,
    Guid? ParentPageId,
    bool IsInternalOnly) : ICommand;
