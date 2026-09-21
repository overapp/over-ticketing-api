using Application.Abstractions.Messaging;

namespace Application.Wiki.CreateProjectWikiPage;

public sealed record CreateProjectWikiPageCommand(
    Guid ProjectId,
    string Title,
    string? Slug,
    string Content,
    Guid? ParentPageId,
    bool IsInternalOnly) : ICommand<Guid>;
