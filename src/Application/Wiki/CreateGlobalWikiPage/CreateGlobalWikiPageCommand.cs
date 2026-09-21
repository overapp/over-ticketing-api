using Application.Abstractions.Messaging;

namespace Application.Wiki.CreateGlobalWikiPage;

public sealed record CreateGlobalWikiPageCommand(
    string Title,
    string? Slug,
    string Content,
    Guid? ParentPageId,
    bool IsInternalOnly) : ICommand<Guid>;
