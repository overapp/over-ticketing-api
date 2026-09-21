using Application.Abstractions.Messaging;

namespace Application.Wiki.GetGlobalWikiPageBySlugOrId;

public sealed record GetGlobalWikiPageBySlugOrIdQuery(string SlugOrId) : IQuery<GlobalWikiPageResponse>;
