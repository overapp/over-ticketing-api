using Application.Abstractions.Messaging;

namespace Application.Wiki.GetProjectWikiPageBySlugOrId;

public sealed record GetProjectWikiPageBySlugOrIdQuery(
    Guid ProjectId,
    string SlugOrId) : IQuery<ProjectWikiPageResponse>;
